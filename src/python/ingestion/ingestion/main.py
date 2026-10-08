"""Điểm vào ASGI của service ingestion.

Hợp đồng vận hành (`.github/services.json` + `.github/scripts/smoke_test.sh`):

* cổng 8000
* healthcheck `/ingestion/health/ready`

Mọi route nằm dưới tiền tố `/ingestion` vì ALB định tuyến theo tiền tố nhưng **không cắt
tiền tố** trước khi chuyển tiếp. Xem ADR-013.

Ghi chú ranh giới: ingestion **không** nối thẳng database. Nó ghi qua API của
corpus-service (docs/00 §4.2). Nếu một PR sau này thêm chuỗi kết nối vào service này,
đó là dấu hiệu ranh giới đang bị phá chứ không phải một tối ưu.
"""

import logging
from collections.abc import AsyncIterator
from contextlib import asynccontextmanager
from typing import Annotated, Final

import httpx2
from fastapi import APIRouter, Depends, FastAPI, Request
from fastapi.responses import JSONResponse
from pydantic import BaseModel

from ingestion.config import Settings

SERVICE_NAME: Final = "ingestion"
PATH_PREFIX: Final = f"/{SERVICE_NAME}"

_log = logging.getLogger(__name__)


class HealthStatus(BaseModel):
    """Thân phản hồi của healthcheck."""

    status: str
    service: str


router = APIRouter(prefix=PATH_PREFIX, tags=["health"])


@router.get("/health/live")
def live() -> HealthStatus:
    """Tiến trình còn sống hay không. Không chạm phụ thuộc ngoài."""
    return HealthStatus(status="live", service=SERVICE_NAME)


def _client(request: Request) -> httpx2.AsyncClient:
    client: httpx2.AsyncClient = request.app.state.client
    return client


@router.get("/health/ready")
async def ready(client: Annotated[httpx2.AsyncClient, Depends(_client)]) -> JSONResponse:
    """Sẵn sàng nhận việc hay chưa.

    Kiểm đúng phụ thuộc mà service này thật sự có: **gọi được corpus-service**. Đó là nơi
    nó ghi dữ liệu (docs/00 §4.2), nên corpus chết thì ingestion không làm được gì.

    Không sẵn sàng thì trả **503**, không phải 200 kèm một trường trạng thái: cả ALB lẫn
    `smoke_test.sh` đều đọc mã trạng thái.

    TODO(E2): thêm kiểm tra hàng đợi SQS khi nó có mặt.
    """
    try:
        phan_hoi = await client.get("/corpus/health/ready")
        phan_hoi.raise_for_status()
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


def create_app(settings: Settings | None = None) -> FastAPI:
    """Dựng ứng dụng.

    ``settings`` để test trỏ vào một corpus giả lập ở địa chỉ của riêng nó; bỏ trống thì
    đọc từ biến môi trường và chết ngay lúc khởi động nếu thiếu.
    """
    cau_hinh = settings or Settings.tu_moi_truong()

    @asynccontextmanager
    async def vong_doi(app: FastAPI) -> AsyncIterator[None]:
        app.state.settings = cau_hinh
        # Một client dùng lại cho cả tiến trình: mở kết nối mới cho từng lần kiểm tra sức
        # khoẻ là tự tạo ra hàng nghìn kết nối TIME_WAIT trên môi trường thật.
        app.state.client = httpx2.AsyncClient(
            base_url=cau_hinh.corpus_base_url,
            timeout=httpx2.Timeout(5.0),
        )
        try:
            yield
        finally:
            await app.state.client.aclose()

    app = FastAPI(
        title="XNK ingestion",
        version="0.1.0",
        docs_url=f"{PATH_PREFIX}/docs",
        openapi_url=f"{PATH_PREFIX}/openapi.json",
        lifespan=vong_doi,
    )
    app.include_router(router)
    return app


# ⚠️ KHÔNG khai `app = create_app()` ở cấp module — service đã có cấu hình bắt buộc, nên
# chỉ import module cũng sẽ đòi biến môi trường. Uvicorn nạp bằng cờ `--factory`; xem CMD
# trong Dockerfile.
