"""Điểm vào ASGI của service ingestion.

Hợp đồng vận hành (`.github/services.json` + `.github/scripts/smoke_test.sh`):

* cổng 8000
* healthcheck `/ingestion/health/ready`

Mọi route nằm dưới tiền tố `/ingestion` vì ALB định tuyến theo tiền tố nhưng **không cắt
tiền tố** trước khi chuyển tiếp. Xem ADR-0005.

Ghi chú ranh giới: ingestion **không** nối thẳng database. Nó ghi qua API của
corpus-service (docs/00 §4.2). Nếu một PR sau này thêm chuỗi kết nối vào service này,
đó là dấu hiệu ranh giới đang bị phá chứ không phải một tối ưu.
"""

from typing import Final

from fastapi import APIRouter, FastAPI
from pydantic import BaseModel

SERVICE_NAME: Final = "ingestion"
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
    """Sẵn sàng nhận việc hay chưa.

    TODO(E2): kiểm tra gọi được corpus-service và đọc được hàng đợi SQS khi hai thứ đó
    có mặt. Hiện chưa có phụ thuộc nào nên readiness trùng liveness.
    """
    return HealthStatus(status="ready", service=SERVICE_NAME)


def create_app() -> FastAPI:
    """Dựng ứng dụng. Tách thành hàm để test dựng được bản sạch cho từng ca."""
    app = FastAPI(
        title="XNK ingestion",
        version="0.1.0",
        docs_url=f"{PATH_PREFIX}/docs",
        openapi_url=f"{PATH_PREFIX}/openapi.json",
    )
    app.include_router(router)
    return app


app = create_app()
