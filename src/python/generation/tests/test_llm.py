"""Đường gọi LLM thật, đối diện một máy chủ ASGI thật.

`test_answer.py` dùng một cài đặt trong bộ nhớ để kiểm phần endpoint. File này kiểm phần
còn lại — thứ mà cài đặt trong bộ nhớ đi vòng qua: **thân request gửi lên có đúng giao thức
OpenAI không**, và phản hồi có được đọc đúng chỗ không.

Không có nó thì một lỗi sai tên trường (``messages`` thành ``message``) vẫn để mọi test
khác xanh, và chỉ lộ ra khi Ollama trả 400 ở môi trường thật.
"""

from __future__ import annotations

from typing import Any

import httpx2
import pytest
from fastapi import FastAPI

from generation.llm import SYSTEM_PROMPT, OpenAiCompatibleClient


def _may_chu_gia() -> tuple[FastAPI, dict[str, Any]]:
    """Một endpoint tương thích OpenAI tối giản, ghi lại request nhận được.

    Route khai dưới ``/v1/...`` vì ``ASGITransport`` chuyển tới ứng dụng **nguyên đường dẫn
    đầy đủ**, gồm cả phần ``/v1`` trong ``base_url`` — đúng như một máy chủ thật nhận được.
    """
    app = FastAPI()
    da_nhan: dict[str, Any] = {}

    @app.post("/v1/chat/completions")
    async def _completions(than: dict[str, Any]) -> dict[str, Any]:
        da_nhan.update(than)
        return {"choices": [{"message": {"role": "assistant", "content": "Trả lời từ mô hình."}}]}

    @app.get("/v1/models")
    async def _models() -> dict[str, Any]:
        return {"object": "list", "data": [{"id": "mo-hinh-dung-cho-test"}]}

    return app, da_nhan


def _client(app: FastAPI) -> httpx2.AsyncClient:
    return httpx2.AsyncClient(
        transport=httpx2.ASGITransport(app=app), base_url="http://llm.test/v1"
    )


async def test_gui_dung_giao_thuc_openai() -> None:
    app, da_nhan = _may_chu_gia()

    async with _client(app) as http:
        ket_qua = await OpenAiCompatibleClient(http, "mo-hinh-dung-cho-test").tra_loi(
            "Thủ tục hải quan gồm những bước nào?",
            ["39/2018/TT-BTC — sửa đổi Thông tư 38/2015"],
        )

    assert ket_qua == "Trả lời từ mô hình."
    assert da_nhan["model"] == "mo-hinh-dung-cho-test"
    assert da_nhan["stream"] is False
    assert da_nhan["messages"][0] == {"role": "system", "content": SYSTEM_PROMPT}
    # Ngữ cảnh phải đi kèm câu hỏi, nếu không mô hình trả lời bằng trí nhớ của nó — đúng
    # thứ mà cả hệ thống này tồn tại để tránh.
    assert "39/2018/TT-BTC" in da_nhan["messages"][1]["content"]
    assert "Thủ tục hải quan gồm những bước nào?" in da_nhan["messages"][1]["content"]


async def test_khong_co_ngu_canh_thi_noi_ro_trong_prompt() -> None:
    app, da_nhan = _may_chu_gia()

    async with _client(app) as http:
        await OpenAiCompatibleClient(http, "m").tra_loi("Câu hỏi trống ngữ cảnh?", [])

    assert "Không có văn bản nào được cung cấp" in da_nhan["messages"][1]["content"]


async def test_san_sang_goi_endpoint_models() -> None:
    app, _ = _may_chu_gia()

    async with _client(app) as http:
        # Không ném ngoại lệ nghĩa là đạt.
        await OpenAiCompatibleClient(http, "m").san_sang()


async def test_phan_hoi_thieu_choices_thi_bao_loi_ro_rang() -> None:
    app = FastAPI()

    @app.post("/v1/chat/completions")
    async def _completions() -> dict[str, Any]:
        return {"error": "mô hình chưa được tải"}

    async with _client(app) as http:
        with pytest.raises(ValueError, match="choices"):
            await OpenAiCompatibleClient(http, "m").tra_loi("Hỏi gì đó?", [])


async def test_mo_hinh_tra_loi_4xx_thi_nem_ngoai_le() -> None:
    app = FastAPI()

    @app.post("/v1/chat/completions")
    async def _completions() -> dict[str, Any]:
        raise httpx2.HTTPError("không tới đây")

    async with _client(app) as http:
        with pytest.raises(Exception):  # noqa: B017 — chỉ cần khẳng định nó KHÔNG im lặng
            await OpenAiCompatibleClient(http, "m").tra_loi("Hỏi gì đó?", [])
