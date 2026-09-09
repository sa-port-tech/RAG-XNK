"""Điểm vào ASGI của service retrieval.

Hợp đồng vận hành (`.github/services.json` + `.github/scripts/smoke_test.sh`):

* cổng 8000
* healthcheck `/retrieval/health/ready` — smoke test sau deploy gọi đúng URL này

Vì sao mọi route nằm dưới tiền tố `/retrieval` thay vì dùng ``root_path``:
ALB định tuyến theo tiền tố đường dẫn nhưng **không cắt tiền tố** trước khi chuyển tiếp,
nên ứng dụng nhận nguyên `/retrieval/...`. Gắn tiền tố thẳng vào router là cách duy nhất
đúng trong cả ba môi trường — chạy trực tiếp lúc dev, sau ALB, và trong test — mà không
phụ thuộc vào hành vi cắt đường dẫn của một lớp proxy nào đó. Xem ADR-0005.

Ranh giới dữ liệu: service này ĐỌC trực tiếp schema ``corpus`` (ngoại lệ ADR-012) và sẽ
đọc-ghi schema ``vector`` khi epic E3 bắt đầu. Nó **không** ghi vào ``corpus``, và vai trò
database ``xnk_retrieval`` chỉ có quyền SELECT ở đó — xem `db/roles.sql`.
"""

import logging
import uuid
from collections.abc import AsyncIterator
from contextlib import asynccontextmanager
from typing import Annotated, Final

from fastapi import APIRouter, Depends, FastAPI, HTTPException, Query, Request
from fastapi.responses import JSONResponse
from pydantic import BaseModel
from sqlalchemy.ext.asyncio import AsyncEngine

from retrieval import db
from retrieval.auth import tenant_tu_request
from retrieval.config import Settings

SERVICE_NAME: Final = "retrieval"
PATH_PREFIX: Final = f"/{SERVICE_NAME}"

_log = logging.getLogger(__name__)

KICH_THUOC_TRANG_MAC_DINH: Final = 20
KICH_THUOC_TRANG_TOI_DA: Final = 100


class HealthStatus(BaseModel):
    """Thân phản hồi của healthcheck.

    Có kiểu tường minh thay vì trả dict để OpenAPI sinh ra lược đồ thật — quyết định
    code-first ở ADR-0003 chỉ có giá trị khi tài liệu sinh ra mô tả đúng cái đang chạy.
    """

    status: str
    service: str


class VanBanTomTat(BaseModel):
    """Văn bản ở mức tóm tắt.

    Không có ``tenant_id``: người gọi đã biết mình thuộc tenant nào, và đưa định danh nội bộ
    ra ngoài là mời người ta tự lọc ở phía client — rồi lớp cách ly thật ở tầng SQL trở
    thành "chỉ là một trong hai chỗ lọc". ``is_shared`` mới là thứ client cần biết.
    """

    id: str
    document_number: str
    title: str
    effective_from: str | None
    effective_to: str | None
    is_shared: bool


class TrangKetQua(BaseModel):
    """Một trang kết quả.

    ``total_count`` đếm **sau khi bộ lọc tenant đã áp**, nên hai tenant gọi cùng một URL sẽ
    nhận hai con số khác nhau. Trả tổng thật của bảng sẽ rò rỉ quy mô dữ liệu của tenant
    khác — không lộ nội dung nào, nhưng vẫn là rò rỉ.
    """

    items: list[VanBanTomTat]
    page: int
    page_size: int
    total_count: int


def _engine(request: Request) -> AsyncEngine:
    engine: AsyncEngine = request.app.state.engine
    return engine


def _settings(request: Request) -> Settings:
    settings: Settings = request.app.state.settings
    return settings


router = APIRouter(prefix=PATH_PREFIX, tags=["health"])


@router.get("/health/live")
def live() -> HealthStatus:
    """Tiến trình còn sống hay không.

    Không chạm phụ thuộc ngoài. Liveness mà gọi database thì một sự cố database sẽ khiến
    orchestrator giết và khởi động lại toàn bộ service — biến sự cố phụ thuộc thành sự cố
    lan rộng.
    """
    return HealthStatus(status="live", service=SERVICE_NAME)


@router.get("/health/ready")
async def ready(engine: Annotated[AsyncEngine, Depends(_engine)]) -> JSONResponse:
    """Sẵn sàng nhận lưu lượng hay chưa.

    Kiểm chính thứ service phụ thuộc: đọc được bảng trong schema ``corpus``. Kiểm bằng
    ``SELECT 1`` đơn thuần thì readiness vẫn xanh khi vai trò thiếu quyền hoặc migration
    chưa chạy — đúng hai tình huống service không phục vụ được gì.

    Không sẵn sàng thì trả **503**, không phải 200 kèm một trường trạng thái. Cả ALB lẫn
    `smoke_test.sh` đều đọc mã trạng thái; báo 200 rồi ghi "not ready" trong thân là nói
    dối đúng nơi mà máy móc đang nghe.
    """
    try:
        await db.kiem_tra_san_sang(engine)
    except Exception as loi:
        # Chi tiết lỗi CHỈ vào log, không ra thân phản hồi. Chuỗi ngoại lệ ở đây thường
        # chứa host, tên database, tên vai trò — đủ để người ngoài vẽ lại sơ đồ hạ tầng
        # từ một endpoint vốn không cần xác thực (CodeQL: information exposure through an
        # exception). Người trực cần chi tiết thì đọc log, chỗ đó mới là của họ.
        _log.warning("Readiness thất bại: %s", loi)
        return JSONResponse(
            status_code=503,
            content={"status": "not-ready", "service": SERVICE_NAME},
        )

    return JSONResponse(
        status_code=200,
        content=HealthStatus(status="ready", service=SERVICE_NAME).model_dump(),
    )


@router.get("/documents", tags=["documents"])
async def liet_ke_van_ban(
    request: Request,
    engine: Annotated[AsyncEngine, Depends(_engine)],
    settings: Annotated[Settings, Depends(_settings)],
    page: Annotated[int, Query(ge=1)] = 1,
    page_size: Annotated[int, Query(ge=1, le=KICH_THUOC_TRANG_TOI_DA)] = KICH_THUOC_TRANG_MAC_DINH,
) -> TrangKetQua:
    """Liệt kê văn bản mà tenant hiện tại được xem.

    ⚠️ Chú ý thứ KHÔNG có trong hàm này: **không một dòng nào lọc theo tenant.** Điều kiện
    nằm trong câu SQL ở `retrieval/db.py` (ADR-012). Thêm một bộ lọc nữa ở đây là **có
    hại**, không phải thừa: nó tạo ấn tượng rằng lọc là việc của tầng ứng dụng, và endpoint
    tiếp theo sẽ được viết với niềm tin đó rồi quên mất một chỗ.
    """
    tenant_id = tenant_tu_request(
        request, settings.jwt_issuer, settings.jwt_audience, settings.jwt_signing_key
    )

    tong = await db.dem_van_ban(engine, tenant_id)
    ban_ghi = await db.liet_ke_van_ban(
        engine, tenant_id, limit=page_size, offset=(page - 1) * page_size
    )

    return TrangKetQua(
        items=[
            VanBanTomTat(
                id=str(v.id),
                document_number=v.document_number,
                title=v.title,
                effective_from=v.effective_from.isoformat() if v.effective_from else None,
                effective_to=v.effective_to.isoformat() if v.effective_to else None,
                is_shared=v.is_shared,
            )
            for v in ban_ghi
        ],
        page=page,
        page_size=page_size,
        total_count=tong,
    )


@router.get("/documents/{document_id}", tags=["documents"])
async def lay_van_ban(
    document_id: uuid.UUID,
    request: Request,
    engine: Annotated[AsyncEngine, Depends(_engine)],
    settings: Annotated[Settings, Depends(_settings)],
) -> VanBanTomTat:
    """Lấy một văn bản theo id.

    Trả **404** cho cả hai trường hợp "id không tồn tại" và "id thuộc tenant khác" — cùng
    một mã lỗi, cùng một thân phản hồi. Trả 403 riêng cho trường hợp thứ hai sẽ xác nhận với
    người gọi rằng id đó CÓ tồn tại, chỉ là không xem được — rò rỉ sự tồn tại của dữ liệu
    tenant khác qua một kênh không phải nội dung.
    """
    tenant_id = tenant_tu_request(
        request, settings.jwt_issuer, settings.jwt_audience, settings.jwt_signing_key
    )

    v = await db.lay_van_ban(engine, tenant_id, document_id)
    if v is None:
        raise HTTPException(status_code=404, detail="document not found")

    return VanBanTomTat(
        id=str(v.id),
        document_number=v.document_number,
        title=v.title,
        effective_from=v.effective_from.isoformat() if v.effective_from else None,
        effective_to=v.effective_to.isoformat() if v.effective_to else None,
        is_shared=v.is_shared,
    )


def create_app(settings: Settings | None = None) -> FastAPI:
    """Dựng ứng dụng.

    ``settings`` để test dựng được bản sạch trỏ vào database của riêng nó; khi bỏ trống thì
    đọc từ biến môi trường và **chết ngay lúc khởi động** nếu thiếu.
    """
    cau_hinh = settings or Settings.tu_moi_truong()

    @asynccontextmanager
    async def vong_doi(app: FastAPI) -> AsyncIterator[None]:
        app.state.settings = cau_hinh
        app.state.engine = db.tao_engine(cau_hinh.database_url)
        try:
            yield
        finally:
            # Đóng pool tường minh: bỏ qua thì mỗi lần dựng app trong test để lại một pool
            # còn sống, và bộ test hết kết nối trước khi chạy hết.
            await app.state.engine.dispose()

    app = FastAPI(
        title="XNK retrieval",
        version="0.1.0",
        docs_url=f"{PATH_PREFIX}/docs",
        openapi_url=f"{PATH_PREFIX}/openapi.json",
        lifespan=vong_doi,
    )
    app.include_router(router)
    return app


# ⚠️ KHÔNG khai `app = create_app()` ở cấp module.
#
# Từ khi service có cấu hình bắt buộc, gọi create_app() lúc import nghĩa là **chỉ cần
# import module cũng đòi đủ bốn biến môi trường** — kể cả khi chỉ muốn đọc PATH_PREFIX
# trong một unit test. Uvicorn nạp bằng cờ `--factory` thay vì trỏ vào một biến toàn cục;
# xem CMD trong Dockerfile của service này.
