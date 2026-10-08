"""Hạ tầng test cho generation."""

from __future__ import annotations

import pytest

from generation.config import Settings

CAU_HINH = Settings(
    llm_base_url="http://llm.khong-ton-tai.invalid/v1",
    llm_model="mo-hinh-dung-cho-test",
    llm_timeout_seconds=5.0,
)


class LlmTrongBoNho:
    """Một cài đặt ``LlmClient`` chạy trong bộ nhớ.

    Đây là **cài đặt thật thứ hai của một Protocol**, không phải một lớp giả lập chắp vá:
    nó tồn tại được là nhờ `generation/llm.py` đã tách interface ra từ đầu. Nhờ vậy mã sản
    phẩm không có một dòng nào phục vụ riêng cho test.

    Đường gọi HTTP thật của ``OpenAiCompatibleClient`` được kiểm riêng ở
    `test_llm.py`, đối diện một máy chủ ASGI thật.
    """

    def __init__(self, cau_tra_loi: str = "Câu trả lời mẫu.", loi: Exception | None = None) -> None:
        self._cau_tra_loi = cau_tra_loi
        self._loi = loi
        self.lan_goi: list[tuple[str, list[str]]] = []

    async def tra_loi(self, cau_hoi: str, ngu_canh: list[str]) -> str:
        self.lan_goi.append((cau_hoi, ngu_canh))
        if self._loi is not None:
            raise self._loi
        return self._cau_tra_loi

    async def san_sang(self) -> None:
        if self._loi is not None:
            raise self._loi


@pytest.fixture
def settings() -> Settings:
    return CAU_HINH
