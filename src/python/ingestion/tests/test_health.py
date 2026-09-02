"""Test khói cho healthcheck.

Không chỉ để `pytest` khỏi rỗng: các test này khoá lại đúng cái hợp đồng mà
`smoke_test.sh` dựa vào sau mỗi lần deploy.
"""

from __future__ import annotations

import httpx2
from fastapi import FastAPI
from fastapi.testclient import TestClient

from ingestion.config import Settings
from ingestion.main import PATH_PREFIX, _client, create_app

CAU_HINH = Settings(corpus_base_url="http://corpus.khong-ton-tai.invalid")


def test_live_tra_ve_200_duoi_dung_tien_to() -> None:
    with TestClient(create_app(CAU_HINH)) as client:
        phan_hoi = client.get(f"{PATH_PREFIX}/health/live")

    assert phan_hoi.status_code == 200
    assert phan_hoi.json() == {"status": "live", "service": "ingestion"}


def test_khong_co_tien_to_thi_404() -> None:
    """ALB không cắt tiền tố, nên route trần phải KHÔNG tồn tại."""
    with TestClient(create_app(CAU_HINH)) as client:
        assert client.get("/health/ready").status_code == 404


def test_ready_tra_ve_503_khi_khong_goi_duoc_corpus() -> None:
    """Corpus chết thì ingestion không ghi được gì, nên nó CHƯA sẵn sàng.

    Đây là phụ thuộc thật của service này: nó ghi qua API của corpus-service, không chạm
    database (docs/00 §4.2).
    """
    khong_toi_duoc = Settings(corpus_base_url="http://127.0.0.1:1")

    with TestClient(create_app(khong_toi_duoc)) as client:
        phan_hoi = client.get(f"{PATH_PREFIX}/health/ready")

    assert phan_hoi.status_code == 503
    assert phan_hoi.json()["status"] == "not-ready"


def test_ready_tra_ve_200_khi_corpus_tra_loi() -> None:
    """Dựng một corpus tối giản ngay trong tiến trình test.

    Dùng ASGI transport của httpx2 để client gọi thẳng vào ứng dụng giả, không mở cổng
    mạng nào. Ghi đè qua ``dependency_overrides`` của FastAPI nên **mã sản phẩm không có
    một dòng nào phục vụ riêng cho test**.
    """
    corpus_gia = FastAPI()

    @corpus_gia.get("/corpus/health/ready")
    def _ready() -> dict[str, str]:
        return {"status": "ready", "service": "corpus"}

    app = create_app(CAU_HINH)
    client_gia = httpx2.AsyncClient(
        transport=httpx2.ASGITransport(app=corpus_gia),
        base_url="http://corpus",
    )
    app.dependency_overrides[_client] = lambda: client_gia

    try:
        with TestClient(app) as client:
            phan_hoi = client.get(f"{PATH_PREFIX}/health/ready")
    finally:
        app.dependency_overrides.clear()

    assert phan_hoi.status_code == 200
    assert phan_hoi.json() == {"status": "ready", "service": "ingestion"}
