"""Cấu hình đọc từ biến môi trường.

Service này **không** có chuỗi kết nối database, và đó là ranh giới chứ không phải thiếu
sót: `docs/00` §4.2 ghi rõ ingestion ghi qua **API của corpus-service**. Nếu một PR sau này
thêm `DATABASE_URL` vào đây, đó là dấu hiệu ranh giới đang bị phá.
"""

from __future__ import annotations

import os
from dataclasses import dataclass


@dataclass(frozen=True, slots=True)
class Settings:
    """Toàn bộ cấu hình mà service ingestion cần."""

    corpus_base_url: str

    @classmethod
    def tu_moi_truong(cls) -> Settings:
        """Đọc cấu hình, báo lỗi rõ ràng khi thiếu."""
        url = os.environ.get("CORPUS_BASE_URL")
        if not url:
            raise RuntimeError(
                "Thiếu biến môi trường CORPUS_BASE_URL — địa chỉ gốc của corpus-service, "
                "ví dụ http://corpus:8080. Chép .env.example thành .env, hoặc xem "
                "docker-compose.yml."
            )

        return cls(corpus_base_url=url.rstrip("/"))
