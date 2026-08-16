"""Test khói cho healthcheck.

Không chỉ để `pytest` khỏi rỗng: hai test này khoá lại đúng cái hợp đồng mà
`smoke_test.sh` dựa vào sau mỗi lần deploy. Đổi tiền tố đường dẫn hay đổi mã trạng thái
sẽ làm đỏ ở đây — tại chỗ, trong 2 giây — thay vì đỏ sau khi image đã lên dev.
"""

from fastapi.testclient import TestClient

from retrieval.main import PATH_PREFIX, create_app

client = TestClient(create_app())


def test_ready_tra_ve_200_duoi_dung_tien_to() -> None:
    response = client.get(f"{PATH_PREFIX}/health/ready")

    assert response.status_code == 200
    assert response.json() == {"status": "ready", "service": "retrieval"}


def test_live_tra_ve_200_duoi_dung_tien_to() -> None:
    response = client.get(f"{PATH_PREFIX}/health/live")

    assert response.status_code == 200
    assert response.json() == {"status": "live", "service": "retrieval"}


def test_khong_co_tien_to_thi_404() -> None:
    """ALB không cắt tiền tố, nên route trần phải KHÔNG tồn tại.

    Nếu test này đỏ nghĩa là ai đó vừa thêm route không tiền tố — và smoke test sau deploy
    sẽ vẫn xanh trong khi ALB chuyển tiếp tới một đường dẫn không ai phục vụ.
    """
    assert client.get("/health/ready").status_code == 404
