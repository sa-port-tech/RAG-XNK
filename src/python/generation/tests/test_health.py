"""Test khói cho healthcheck — khoá hợp đồng mà smoke_test.sh dựa vào sau mỗi lần deploy."""

from fastapi.testclient import TestClient

from generation.main import PATH_PREFIX, create_app

client = TestClient(create_app())


def test_ready_tra_ve_200_duoi_dung_tien_to() -> None:
    response = client.get(f"{PATH_PREFIX}/health/ready")

    assert response.status_code == 200
    assert response.json() == {"status": "ready", "service": "generation"}


def test_live_tra_ve_200_duoi_dung_tien_to() -> None:
    response = client.get(f"{PATH_PREFIX}/health/live")

    assert response.status_code == 200
    assert response.json() == {"status": "live", "service": "generation"}


def test_khong_co_tien_to_thi_404() -> None:
    """ALB không cắt tiền tố, nên route trần phải KHÔNG tồn tại."""
    assert client.get("/health/ready").status_code == 404
