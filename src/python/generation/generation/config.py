"""Cấu hình đọc từ biến môi trường.

Service này **không sở hữu dữ liệu nào** (docs/00 §4.2), nên không có chuỗi kết nối
database. Phụ thuộc duy nhất của nó là mô hình ngôn ngữ.
"""

from __future__ import annotations

import os
from dataclasses import dataclass


@dataclass(frozen=True, slots=True)
class Settings:
    """Toàn bộ cấu hình mà service generation cần."""

    llm_base_url: str
    llm_model: str
    llm_timeout_seconds: float

    @classmethod
    def tu_moi_truong(cls) -> Settings:
        """Đọc cấu hình, báo lỗi một lần cho tất cả biến còn thiếu."""
        can_co = {
            "LLM_BASE_URL": "địa chỉ gốc của endpoint tương thích OpenAI, ví dụ http://ollama:11434/v1",
            "LLM_MODEL": "tên mô hình, ví dụ qwen2.5:3b-instruct",
        }

        thieu = [f"  · {ten}: {mo_ta}" for ten, mo_ta in can_co.items() if not os.environ.get(ten)]
        if thieu:
            raise RuntimeError(
                "Thiếu biến môi trường bắt buộc:\n"
                + "\n".join(thieu)
                + "\n\nChép .env.example thành .env, hoặc xem docker-compose.yml."
            )

        return cls(
            llm_base_url=os.environ["LLM_BASE_URL"].rstrip("/"),
            llm_model=os.environ["LLM_MODEL"],
            # Mô hình chạy trên CPU của máy dev có thể mất hàng chục giây cho một câu trả
            # lời. Ngưỡng rộng, nhưng vẫn hữu hạn: một request treo vô hạn sẽ giữ kết nối
            # và cuối cùng làm nghẽn cả service.
            llm_timeout_seconds=float(os.environ.get("LLM_TIMEOUT_SECONDS", "120")),
        )
