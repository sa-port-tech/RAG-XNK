"""Điểm vào ASGI của service retrieval.

Hợp đồng vận hành (`.github/services.json` + `.github/scripts/smoke_test.sh`):

* cổng 8000
* healthcheck `/retrieval/health/ready` — smoke test sau deploy gọi đúng URL này

Vì sao mọi route nằm dưới tiền tố `/retrieval` thay vì dùng ``root_path``:
ALB định tuyến theo tiền tố đường dẫn nhưng **không cắt tiền tố** trước khi chuyển tiếp,
nên ứng dụng nhận nguyên `/retrieval/...`. Gắn tiền tố thẳng vào router là cách duy nhất
đúng trong cả ba môi trường — chạy trực tiếp lúc dev, sau ALB, và trong test — mà không
phụ thuộc vào hành vi cắt đường dẫn của một lớp proxy nào đó. Xem ADR-0005.
"""

from typing import Final

from fastapi import APIRouter, FastAPI
from pydantic import BaseModel

SERVICE_NAME: Final = "retrieval"
PATH_PREFIX: Final = f"/{SERVICE_NAME}"


class HealthStatus(BaseModel):
    """Thân phản hồi của healthcheck.

    Có kiểu tường minh thay vì trả dict để OpenAPI sinh ra lược đồ thật — quyết định
    code-first ở ADR-0003 chỉ có giá trị khi tài liệu sinh ra mô tả đúng cái đang chạy.
    """

    status: str
    service: str


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
def ready() -> HealthStatus:
    """Sẵn sàng nhận lưu lượng hay chưa.

    TODO(E3): kiểm tra kết nối tới schema `corpus` và `vector` khi lớp dữ liệu có mặt.
    Chừng nào chưa có phụ thuộc nào, readiness trùng liveness — và nói thẳng điều đó
    ra đây tốt hơn là để người sau tưởng nó đã kiểm tra thứ gì.
    """
    return HealthStatus(status="ready", service=SERVICE_NAME)


def create_app() -> FastAPI:
    """Dựng ứng dụng. Tách thành hàm để test dựng được bản sạch cho từng ca."""
    app = FastAPI(
        title="XNK retrieval",
        version="0.1.0",
        docs_url=f"{PATH_PREFIX}/docs",
        openapi_url=f"{PATH_PREFIX}/openapi.json",
    )
    app.include_router(router)
    return app


app = create_app()
