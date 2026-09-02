"""Test khói cho healthcheck.

Không chỉ để `pytest` khỏi rỗng: các test này khoá lại đúng cái hợp đồng mà
`smoke_test.sh` dựa vào sau mỗi lần deploy.
"""

from __future__ import annotations

from fastapi.testclient import TestClient

from generation.config import Settings
from generation.main import PATH_PREFIX, create_app
from tests.conftest import LlmTrongBoNho


def test_live_tra_ve_200_duoi_dung_tien_to(settings: Settings) -> None:
    with TestClient(create_app(settings, llm=LlmTrongBoNho())) as client:
        phan_hoi = client.get(f"{PATH_PREFIX}/health/live")

    assert phan_hoi.status_code == 200
    assert phan_hoi.json() == {"status": "live", "service": "generation"}


def test_khong_co_tien_to_thi_404(settings: Settings) -> None:
    """ALB không cắt tiền tố, nên route trần phải KHÔNG tồn tại."""
    with TestClient(create_app(settings, llm=LlmTrongBoNho())) as client:
        assert client.get("/health/ready").status_code == 404


def test_ready_tra_ve_200_khi_goi_duoc_mo_hinh(settings: Settings) -> None:
    with TestClient(create_app(settings, llm=LlmTrongBoNho())) as client:
        phan_hoi = client.get(f"{PATH_PREFIX}/health/ready")

    assert phan_hoi.status_code == 200
    assert phan_hoi.json()["status"] == "ready"


def test_ready_tra_ve_503_khi_khong_goi_duoc_mo_hinh(settings: Settings) -> None:
    """Không có mô hình thì service không trả lời được gì, nên nó CHƯA sẵn sàng."""
    hong = LlmTrongBoNho(loi=ConnectionError("không nối được"))

    with TestClient(create_app(settings, llm=hong)) as client:
        phan_hoi = client.get(f"{PATH_PREFIX}/health/ready")

    assert phan_hoi.status_code == 503
    assert phan_hoi.json()["status"] == "not-ready"


def test_live_van_xanh_khi_mo_hinh_hong(settings: Settings) -> None:
    """Liveness KHÔNG chạm phụ thuộc ngoài.

    Liveness mà gọi mô hình thì một sự cố Ollama sẽ khiến orchestrator giết và khởi động
    lại service — biến sự cố phụ thuộc thành sự cố lan rộng.
    """
    hong = LlmTrongBoNho(loi=ConnectionError("không nối được"))

    with TestClient(create_app(settings, llm=hong)) as client:
        assert client.get(f"{PATH_PREFIX}/health/live").status_code == 200
