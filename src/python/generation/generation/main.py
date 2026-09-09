"""Điểm vào ASGI của service generation.

Hợp đồng vận hành (`.github/services.json` + `.github/scripts/smoke_test.sh`):

* cổng 8000
* healthcheck `/generation/health/ready`

Mọi route nằm dưới tiền tố `/generation` vì ALB định tuyến theo tiền tố nhưng **không cắt
tiền tố** trước khi chuyển tiếp. Xem ADR-013.

Ghi chú ranh giới: generation **không sở hữu schema nào** (docs/00 §4.2). Nó nhận ngữ
cảnh đã truy xuất, gọi mô hình ngôn ngữ, áp guardrail, trả câu trả lời kèm trích dẫn.
Nếu service này bắt đầu tự truy vấn corpus, nghĩa là bộ lọc hiệu lực đã bị đi vòng — đó
là lớp phòng thủ số một của hệ thống (docs/00 §10.3).
"""

import logging
from collections.abc import AsyncIterator
from contextlib import asynccontextmanager
from typing import Annotated, Final

import httpx2
from fastapi import APIRouter, Depends, FastAPI, HTTPException, Request, status
from fastapi.responses import JSONResponse
from pydantic import BaseModel, Field

from generation.config import Settings
from generation.llm import LlmClient, OpenAiCompatibleClient

SERVICE_NAME: Final = "generation"
PATH_PREFIX: Final = f"/{SERVICE_NAME}"

_log = logging.getLogger(__name__)


class HealthStatus(BaseModel):
    """Thân phản hồi của healthcheck."""

    status: str
    service: str


class YeuCauTraLoi(BaseModel):
    """Câu hỏi kèm ngữ cảnh đã được truy xuất."""

    question: str = Field(min_length=1, max_length=2000)
    # Trích đoạn văn bản do `retrieval` chọn ra. Service này KHÔNG tự đi tìm — xem ghi chú
    # ranh giới ở đầu file.
    context: list[str] = Field(default_factory=list, max_length=50)


class PhanHoiTraLoi(BaseModel):
    """Câu trả lời sinh ra."""

    answer: str
    model: str


def _llm(request: Request) -> LlmClient:
    client: LlmClient = request.app.state.llm
    return client


def _settings(request: Request) -> Settings:
    settings: Settings = request.app.state.settings
    return settings


router = APIRouter(prefix=PATH_PREFIX, tags=["health"])


@router.get("/health/live")
def live() -> HealthStatus:
    """Tiến trình còn sống hay không. Không chạm phụ thuộc ngoài."""
    return HealthStatus(status="live", service=SERVICE_NAME)


@router.get("/health/ready")
async def ready(llm: Annotated[LlmClient, Depends(_llm)]) -> JSONResponse:
    """Sẵn sàng nhận lưu lượng hay chưa.

    Phụ thuộc duy nhất của service này là mô hình ngôn ngữ. Không gọi được nó thì mọi
    request đều hỏng, nên readiness phải **đỏ** — trả 200 kèm một trường trạng thái là nói
    dối đúng nơi mà ALB và `smoke_test.sh` đang nghe.

    TODO(E4): kèm theo đó, kiểm nạp được system prompt từ `prompts/`.
    """
    try:
        await llm.san_sang()
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


@router.post("/answer", tags=["generation"])
async def tra_loi(
    yeu_cau: YeuCauTraLoi,
    llm: Annotated[LlmClient, Depends(_llm)],
    settings: Annotated[Settings, Depends(_settings)],
) -> PhanHoiTraLoi:
    """Sinh câu trả lời từ câu hỏi và ngữ cảnh đã truy xuất.

    Mô hình hỏng thì trả **502**, không phải 500: lỗi nằm ở dịch vụ phía sau, và phân biệt
    được hai loại đó là thứ giúp người trực đêm biết nên đi xem log của ai.
    """
    try:
        cau_tra_loi = await llm.tra_loi(yeu_cau.question, yeu_cau.context)
    except Exception as loi:
        # Chi tiết vào log, không ra ngoài: thông báo lỗi của tầng HTTP thường mang theo
        # địa chỉ endpoint và tên mô hình.
        _log.exception("Gọi mô hình ngôn ngữ thất bại")
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail="Không gọi được mô hình ngôn ngữ.",
        ) from loi

    return PhanHoiTraLoi(answer=cau_tra_loi, model=settings.llm_model)


def create_app(settings: Settings | None = None, llm: LlmClient | None = None) -> FastAPI:
    """Dựng ứng dụng.

    ``llm`` cho phép test thay cài đặt LLM bằng một bản trong bộ nhớ — nhờ Protocol ở
    `generation/llm.py`, việc đó không cần một dòng ``if is_test`` nào trong mã sản phẩm.
    """
    cau_hinh = settings or Settings.tu_moi_truong()

    @asynccontextmanager
    async def vong_doi(app: FastAPI) -> AsyncIterator[None]:
        app.state.settings = cau_hinh

        if llm is not None:
            app.state.llm = llm
            app.state.http = None
        else:
            # Một client dùng lại cho cả tiến trình: mở kết nối mới cho từng request là tự
            # tạo ra hàng nghìn kết nối TIME_WAIT trên môi trường thật.
            app.state.http = httpx2.AsyncClient(
                base_url=cau_hinh.llm_base_url,
                timeout=httpx2.Timeout(cau_hinh.llm_timeout_seconds),
            )
            app.state.llm = OpenAiCompatibleClient(app.state.http, cau_hinh.llm_model)

        try:
            yield
        finally:
            if app.state.http is not None:
                await app.state.http.aclose()

    app = FastAPI(
        title="XNK generation",
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
