"""Test khói cho healthcheck.

Không chỉ để `pytest` khỏi rỗng: các test này khoá lại đúng cái hợp đồng mà
`smoke_test.sh` dựa vào sau mỗi lần deploy. Đổi tiền tố đường dẫn hay đổi mã trạng thái sẽ
làm đỏ ở đây — tại chỗ, trong vài giây — thay vì đỏ sau khi image đã lên dev.
"""

from __future__ import annotations

import pytest
from fastapi.testclient import TestClient

from retrieval.config import Settings
from retrieval.main import PATH_PREFIX, create_app


def test_live_tra_ve_200_duoi_dung_tien_to(settings: Settings) -> None:
    with TestClient(create_app(settings)) as client:
        phan_hoi = client.get(f"{PATH_PREFIX}/health/live")

    assert phan_hoi.status_code == 200
    assert phan_hoi.json() == {"status": "live", "service": "retrieval"}


def test_khong_co_tien_to_thi_404(settings: Settings) -> None:
    """ALB không cắt tiền tố, nên route trần phải KHÔNG tồn tại.

    Nếu test này đỏ nghĩa là ai đó vừa thêm route không tiền tố — và smoke test sau deploy
    sẽ vẫn xanh trong khi ALB chuyển tiếp tới một đường dẫn không ai phục vụ.

    ⚠️ Ba service .NET **không** có tính chất này: chúng dùng ``UsePathBase``, vốn chỉ cắt
    tiền tố khi nó có mặt, nên ở đó đường trần cũng chạy. Cả hai đều đúng sau ALB — đừng
    "sửa cho giống nhau" theo một trong hai chiều.
    """
    with TestClient(create_app(settings)) as client:
        assert client.get("/health/ready").status_code == 404


@pytest.mark.usefixtures("engine")
def test_ready_tra_ve_200_khi_doc_duoc_schema_corpus(settings: Settings) -> None:
    with TestClient(create_app(settings)) as client:
        phan_hoi = client.get(f"{PATH_PREFIX}/health/ready")

    assert phan_hoi.status_code == 200
    assert phan_hoi.json()["status"] == "ready"


def test_ready_tra_ve_503_khi_khong_noi_duoc_database(settings: Settings) -> None:
    """Thiếu phụ thuộc thì readiness phải ĐỎ.

    Trả 200 kèm một trường trạng thái "not ready" là nói dối đúng nơi mà máy móc đang
    nghe: cả ALB lẫn `smoke_test.sh` đều chỉ đọc mã trạng thái.
    """
    hong = Settings(
        # Cổng 1 không có gì lắng nghe — kết nối bị từ chối ngay, không phải chờ hết giờ.
        database_url="postgresql+asyncpg://khong:co@127.0.0.1:1/khong-co",
        jwt_issuer=settings.jwt_issuer,
        jwt_audience=settings.jwt_audience,
        jwt_signing_key=settings.jwt_signing_key,
    )

    with TestClient(create_app(hong)) as client:
        phan_hoi = client.get(f"{PATH_PREFIX}/health/ready")

    assert phan_hoi.status_code == 503
    assert phan_hoi.json()["status"] == "not-ready"


def test_live_van_xanh_khi_database_hong(settings: Settings) -> None:
    """Liveness KHÔNG chạm phụ thuộc ngoài.

    Liveness mà gọi database thì một sự cố database sẽ khiến orchestrator giết và khởi động
    lại toàn bộ service — biến sự cố phụ thuộc thành sự cố lan rộng.
    """
    hong = Settings(
        database_url="postgresql+asyncpg://khong:co@127.0.0.1:1/khong-co",
        jwt_issuer=settings.jwt_issuer,
        jwt_audience=settings.jwt_audience,
        jwt_signing_key=settings.jwt_signing_key,
    )

    with TestClient(create_app(hong)) as client:
        assert client.get(f"{PATH_PREFIX}/health/live").status_code == 200
