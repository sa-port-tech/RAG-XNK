"""Truy cập schema ``corpus``.

Ranh giới quyền
---------------
`retrieval` được cấp quyền **ĐỌC** trực tiếp schema ``corpus`` — ngoại lệ có chủ đích của
`docs/00` §4.4, chốt ở ADR-012: lọc hiệu lực phải nằm trong **cùng một câu SQL** với vector
search, tách ra sẽ phá lớp phòng thủ quan trọng nhất của hệ thống (§10.3).

Ngoại lệ đó cũng được thực thi ở tầng database, không chỉ ghi trong tài liệu: vai trò
``xnk_retrieval`` trong `db/roles.sql` chỉ có ``SELECT`` trên ``corpus``. Một câu ``INSERT``
lọt vào đây sẽ bị PostgreSQL từ chối, không phải chờ người review bắt.

Cách ly tenant
--------------
Điều kiện tenant nằm **trong câu SQL**, không lọc sau khi lấy về. ADR-012 áp cho cả Python,
không chỉ cho EF Core: lọc ở tầng ứng dụng thì chỉ cần một chỗ quên gọi hàm lọc là rò rỉ,
và chỗ quên đó không hiện ra trong bất kỳ diff nào.
"""

from __future__ import annotations

import uuid
from dataclasses import dataclass
from typing import Any, Final

from sqlalchemy import text
from sqlalchemy.ext.asyncio import AsyncEngine, create_async_engine

# Ngữ nghĩa giống hệt global query filter của CorpusDbContext: thấy văn bản dùng chung
# (TenantId IS NULL) cộng văn bản của chính tenant mình.
#
# `tenant_id` ở đây LUÔN có giá trị: token thiếu claim `tenant_id` bị `retrieval/auth.py`
# từ chối bằng 401 trước khi tới được tầng này (xem CLAIM_BAT_BUOC). Trước đây nó nhận
# ``None`` và khi ấy câu SQL rút về "chỉ phần dùng chung" — an toàn, nhưng im lặng.
#
# Tên cột để trong nháy kép vì EF Core sinh lược đồ với tên PascalCase; bỏ nháy thì
# PostgreSQL hạ về chữ thường và không tìm thấy cột nào.
DIEU_KIEN_TENANT: Final = '("TenantId" IS NULL OR "TenantId" = :tenant_id)'

CAU_LIET_KE: Final = f"""
    SELECT "Id", "DocumentNumber", "Title", "EffectiveFrom", "EffectiveTo",
           "TenantId" IS NULL AS is_shared
    FROM corpus.documents
    WHERE {DIEU_KIEN_TENANT}
    ORDER BY "DocumentNumber", "Id"
    LIMIT :limit OFFSET :offset
"""

CAU_DEM: Final = f"""
    SELECT count(*) FROM corpus.documents WHERE {DIEU_KIEN_TENANT}
"""

CAU_LAY_MOT: Final = f"""
    SELECT "Id", "DocumentNumber", "Title", "EffectiveFrom", "EffectiveTo",
           "TenantId" IS NULL AS is_shared
    FROM corpus.documents
    WHERE "Id" = :document_id AND {DIEU_KIEN_TENANT}
"""

# Readiness kiểm chính thứ service này phụ thuộc: đọc được bảng trong schema `corpus`.
# `SELECT 1` đơn thuần chỉ chứng minh kết nối còn sống — nó vẫn xanh khi vai trò thiếu
# quyền hoặc migration chưa chạy, tức là đúng hai tình huống service không phục vụ được.
CAU_KIEM_TRA: Final = "SELECT 1 FROM corpus.documents LIMIT 1"


@dataclass(frozen=True, slots=True)
class VanBan:
    """Một dòng kết quả."""

    id: uuid.UUID
    document_number: str
    title: str
    effective_from: Any
    effective_to: Any
    is_shared: bool


def tao_engine(database_url: str) -> AsyncEngine:
    """Dựng engine async.

    ``pool_pre_ping`` bật vì kết nối rỗi bị PostgreSQL hoặc lớp mạng ở giữa đóng là chuyện
    bình thường; không có nó thì request đầu tiên sau một quãng nghỉ sẽ hỏng một cách khó
    hiểu thay vì được thử lại lặng lẽ.
    """
    return create_async_engine(database_url, pool_pre_ping=True, pool_size=5, max_overflow=5)


async def kiem_tra_san_sang(engine: AsyncEngine) -> None:
    """Ném ngoại lệ nếu không đọc được schema ``corpus``."""
    async with engine.connect() as conn:
        await conn.execute(text(CAU_KIEM_TRA))


async def dem_van_ban(engine: AsyncEngine, tenant_id: uuid.UUID) -> int:
    """Đếm số văn bản mà tenant được phép thấy."""
    async with engine.connect() as conn:
        ket_qua = await conn.execute(text(CAU_DEM), {"tenant_id": tenant_id})
        return int(ket_qua.scalar_one())


async def lay_van_ban(
    engine: AsyncEngine,
    tenant_id: uuid.UUID,
    document_id: uuid.UUID,
) -> VanBan | None:
    """Lấy một văn bản theo id, hoặc ``None`` nếu không có hoặc tenant không được xem.

    Điều kiện ``document_id`` và điều kiện tenant nằm trong CÙNG một câu SQL, không tách
    thành "lấy theo id rồi lọc tenant sau" — id của tenant khác trả về ``None`` giống hệt id
    không tồn tại, và người gọi không phân biệt được hai trường hợp. Đó là chủ đích, cùng lý
    do ADR-012 nêu ở đầu file: tách điều kiện ra khỏi câu SQL là chỗ rò rỉ dễ quên nhất.
    """
    async with engine.connect() as conn:
        ket_qua = await conn.execute(
            text(CAU_LAY_MOT), {"tenant_id": tenant_id, "document_id": document_id}
        )
        dong = ket_qua.first()
        if dong is None:
            return None
        return VanBan(
            id=dong.Id,
            document_number=dong.DocumentNumber,
            title=dong.Title,
            effective_from=dong.EffectiveFrom,
            effective_to=dong.EffectiveTo,
            is_shared=dong.is_shared,
        )


async def liet_ke_van_ban(
    engine: AsyncEngine,
    tenant_id: uuid.UUID,
    limit: int,
    offset: int,
) -> list[VanBan]:
    """Liệt kê văn bản mà tenant được phép thấy, đã phân trang."""
    async with engine.connect() as conn:
        ket_qua = await conn.execute(
            text(CAU_LIET_KE),
            {"tenant_id": tenant_id, "limit": limit, "offset": offset},
        )
        return [
            VanBan(
                id=dong.Id,
                document_number=dong.DocumentNumber,
                title=dong.Title,
                effective_from=dong.EffectiveFrom,
                effective_to=dong.EffectiveTo,
                is_shared=dong.is_shared,
            )
            for dong in ket_qua
        ]
