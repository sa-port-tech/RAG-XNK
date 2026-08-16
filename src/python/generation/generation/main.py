"""Điểm vào ASGI của service generation.

Hợp đồng vận hành (`.github/services.json` + `.github/scripts/smoke_test.sh`):

* cổng 8000
* healthcheck `/generation/health/ready`

Mọi route nằm dưới tiền tố `/generation` vì ALB định tuyến theo tiền tố nhưng **không cắt
tiền tố** trước khi chuyển tiếp. Xem ADR-0005.

Ghi chú ranh giới: generation **không sở hữu schema nào** (docs/00 §4.2). Nó nhận ngữ
cảnh đã truy xuất, gọi Claude qua Bedrock, áp guardrail, trả câu trả lời kèm trích dẫn.
Nếu service này bắt đầu tự truy vấn corpus, nghĩa là bộ lọc hiệu lực đã bị đi vòng — đó
là lớp phòng thủ số một của hệ thống (docs/00 §10.3).
"""

from typing import Final

from fastapi import APIRouter, FastAPI
from pydantic import BaseModel

SERVICE_NAME: Final = "generation"
PATH_PREFIX: Final = f"/{SERVICE_NAME}"


class HealthStatus(BaseModel):
    """Thân phản hồi của healthcheck."""

    status: str
    service: str


router = APIRouter(prefix=PATH_PREFIX, tags=["health"])


@router.get("/health/live")
def live() -> HealthStatus:
    """Tiến trình còn sống hay không. Không chạm phụ thuộc ngoài."""
    return HealthStatus(status="live", service=SERVICE_NAME)


@router.get("/health/ready")
def ready() -> HealthStatus:
    """Sẵn sàng nhận lưu lượng hay chưa.

    TODO(E4): kiểm tra gọi được Bedrock và nạp được system prompt từ `prompts/`.
    Hiện chưa có phụ thuộc nào nên readiness trùng liveness.
    """
    return HealthStatus(status="ready", service=SERVICE_NAME)


def create_app() -> FastAPI:
    """Dựng ứng dụng. Tách thành hàm để test dựng được bản sạch cho từng ca."""
    app = FastAPI(
        title="XNK generation",
        version="0.1.0",
        docs_url=f"{PATH_PREFIX}/docs",
        openapi_url=f"{PATH_PREFIX}/openapi.json",
    )
    app.include_router(router)
    return app


app = create_app()
