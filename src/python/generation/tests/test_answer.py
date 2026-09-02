"""Endpoint sinh câu trả lời."""

from __future__ import annotations

from fastapi.testclient import TestClient

from generation.config import Settings
from generation.main import PATH_PREFIX, create_app
from tests.conftest import LlmTrongBoNho


def test_tra_ve_cau_tra_loi_va_ten_mo_hinh(settings: Settings) -> None:
    llm = LlmTrongBoNho("Theo Thông tư 39/2018/TT-BTC…")

    with TestClient(create_app(settings, llm=llm)) as client:
        phan_hoi = client.post(
            f"{PATH_PREFIX}/answer",
            json={
                "question": "Thủ tục hải quan gồm những bước nào?",
                "context": ["39/2018/TT-BTC"],
            },
        )

    assert phan_hoi.status_code == 200
    assert phan_hoi.json() == {
        "answer": "Theo Thông tư 39/2018/TT-BTC…",
        "model": settings.llm_model,
    }
    # Ngữ cảnh phải được chuyển xuống mô hình, không bị bỏ rơi giữa đường.
    assert llm.lan_goi == [("Thủ tục hải quan gồm những bước nào?", ["39/2018/TT-BTC"])]


def test_mo_hinh_hong_thi_502_chu_khong_phai_500(settings: Settings) -> None:
    """Lỗi nằm ở dịch vụ phía sau, và phân biệt được hai loại đó giúp người trực biết đi
    xem log của ai."""
    llm = LlmTrongBoNho(loi=ConnectionError("Ollama chưa chạy"))

    with TestClient(create_app(settings, llm=llm)) as client:
        phan_hoi = client.post(
            f"{PATH_PREFIX}/answer", json={"question": "Hỏi gì đó?", "context": []}
        )

    assert phan_hoi.status_code == 502


def test_cau_hoi_rong_thi_422(settings: Settings) -> None:
    with TestClient(create_app(settings, llm=LlmTrongBoNho())) as client:
        phan_hoi = client.post(f"{PATH_PREFIX}/answer", json={"question": "", "context": []})

    assert phan_hoi.status_code == 422


def test_khong_co_ngu_canh_van_goi_duoc(settings: Settings) -> None:
    """Trả lời khi không có văn bản nào là việc của mô hình, không phải lỗi request.

    Guardrail "từ chối khi không đủ căn cứ" thuộc epic E4 (docs/09 E4-05) và sẽ nằm trong
    prompt cùng lớp kiểm hậu kỳ — không phải một ràng buộc kiểu dữ liệu ở đây.
    """
    llm = LlmTrongBoNho("Chưa đủ căn cứ để trả lời.")

    with TestClient(create_app(settings, llm=llm)) as client:
        phan_hoi = client.post(f"{PATH_PREFIX}/answer", json={"question": "Hỏi gì đó?"})

    assert phan_hoi.status_code == 200
    assert llm.lan_goi == [("Hỏi gì đó?", [])]
