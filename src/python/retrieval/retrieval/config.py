"""Cấu hình đọc từ biến môi trường.

Không dùng giá trị mặc định cho những thứ quyết định đúng/sai về bảo mật (khoá ký, issuer,
audience, chuỗi kết nối). Một giá trị mặc định ở đó là giá trị sẽ đi thẳng lên môi trường
thật vào một ngày nào đó, và không ai nhớ nó tồn tại — cùng lý do đã ghi ở
`Xnk.Shared/Authentication/JwtOptions.cs` phía .NET.

Thiếu biến thì service **chết lúc khởi động** kèm danh sách tên biến còn thiếu, thay vì
chạy được rồi hỏng ở request đầu tiên có token.
"""

from __future__ import annotations

import os
from dataclasses import dataclass
from typing import Final

# Tối thiểu 32 byte cho HMAC-SHA256 — khớp ràng buộc MinLength(32) của JwtOptions bên .NET.
# Hai phía phải cùng một khoá thì token mới dùng chéo được, nên ràng buộc cũng phải giống.
DO_DAI_KHOA_TOI_THIEU: Final = 32


@dataclass(frozen=True, slots=True)
class Settings:
    """Toàn bộ cấu hình mà service retrieval cần."""

    database_url: str
    jwt_issuer: str
    jwt_audience: str
    jwt_signing_key: str

    @classmethod
    def tu_moi_truong(cls) -> Settings:
        """Đọc cấu hình, báo lỗi một lần cho tất cả biến còn thiếu."""
        can_co = {
            "DATABASE_URL": "chuỗi kết nối PostgreSQL (vai trò xnk_retrieval)",
            "XNK_JWT_ISSUER": "issuer của token, phải trùng identity-tenant",
            "XNK_JWT_AUDIENCE": "audience của token",
            "XNK_JWT_SIGNING_KEY": "khoá ký đối xứng, tối thiểu 32 ký tự (ADR-014)",
        }

        thieu = [f"  · {ten}: {mo_ta}" for ten, mo_ta in can_co.items() if not os.environ.get(ten)]
        if thieu:
            raise RuntimeError(
                "Thiếu biến môi trường bắt buộc:\n"
                + "\n".join(thieu)
                + "\n\nChép .env.example thành .env, hoặc xem docker-compose.yml."
            )

        khoa = os.environ["XNK_JWT_SIGNING_KEY"]
        if len(khoa) < DO_DAI_KHOA_TOI_THIEU:
            raise RuntimeError(
                f"XNK_JWT_SIGNING_KEY phải dài tối thiểu {DO_DAI_KHOA_TOI_THIEU} ký tự "
                f"cho HMAC-SHA256, hiện có {len(khoa)}."
            )

        return cls(
            database_url=chuan_hoa_dsn(os.environ["DATABASE_URL"]),
            jwt_issuer=os.environ["XNK_JWT_ISSUER"],
            jwt_audience=os.environ["XNK_JWT_AUDIENCE"],
            jwt_signing_key=khoa,
        )


def chuan_hoa_dsn(dsn: str) -> str:
    """Đổi DSN dạng ``postgres://`` sang dạng SQLAlchemy + asyncpg hiểu được.

    `.env` và mọi công cụ dòng lệnh của PostgreSQL dùng ``postgres://`` hoặc
    ``postgresql://``. SQLAlchemy cần biết cả **driver**, nên nó đòi
    ``postgresql+asyncpg://``. Chuyển ở đây thay vì bắt mỗi nơi khai một dạng riêng: một
    biến DATABASE_URL dùng được cho cả `psql`, `db/apply-migrations.sh` và service này.
    """
    for tien_to in ("postgresql+asyncpg://",):
        if dsn.startswith(tien_to):
            return dsn

    for tien_to in ("postgresql://", "postgres://"):
        if dsn.startswith(tien_to):
            return "postgresql+asyncpg://" + dsn[len(tien_to) :]

    raise RuntimeError(f"DATABASE_URL không phải DSN PostgreSQL hợp lệ: {dsn[:32]}…")
