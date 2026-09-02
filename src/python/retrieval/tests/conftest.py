"""Hạ tầng test cho retrieval: một PostgreSQL thật, đã áp lược đồ.

Hai chế độ, chọn tự động — **cùng khuôn với `PostgresFixture` bên .NET**, và cùng lý do:

* Có ``XNK_TEST_DATABASE_URL`` → dùng luôn. Đây là đường đi trên CI (ci-python cấp một
  service container) và trong container test.
* Không có → tự khởi container cùng ảnh ``pgvector/pgvector:pg16``. Đây là đường đi trên
  máy dev: chạy ``uv run pytest`` ngay sau khi clone, không cần dựng gì trước.

Vì sao là database thật chứ không phải SQLite hay một lớp giả: thứ cần chứng minh là bộ lọc
tenant **nằm trong câu SQL** chạy trên PostgreSQL. Một lớp giả sẽ xanh kể cả khi câu SQL sai.

Lược đồ áp bằng chính ``db/migrations/*.sql`` — file mà ADR-010 gọi là hợp đồng liên ngôn
ngữ. Nhờ vậy bộ test này đồng thời là phép kiểm chứng rằng hợp đồng đó áp được lên một
database trống, y như bộ test .NET làm với ``Database.MigrateAsync()``.
"""

from __future__ import annotations

import os
import uuid
from collections.abc import AsyncIterator, Iterator
from pathlib import Path

import pytest
import pytest_asyncio
from sqlalchemy import text
from sqlalchemy.ext.asyncio import AsyncEngine

from retrieval import db
from retrieval.config import Settings, chuan_hoa_dsn

GOC_REPO = Path(__file__).resolve().parents[4]
THU_MUC_MIGRATION = GOC_REPO / "db" / "migrations"

BIEN_MOI_TRUONG = "XNK_TEST_DATABASE_URL"
ANH_POSTGRES = "pgvector/pgvector:pg16"

KHOA_KY_TEST = "khoa-ky-danh-rieng-cho-test-dai-32-ky-tu"
ISSUER_TEST = "https://identity.test.local"
AUDIENCE_TEST = "xnk-api-test"


@pytest.fixture(scope="session")
def database_url() -> Iterator[str]:
    """Chuỗi kết nối tới PostgreSQL dùng cho test."""
    co_san = os.environ.get(BIEN_MOI_TRUONG)
    if co_san:
        yield co_san
        return

    try:
        from testcontainers.postgres import PostgresContainer
    except ImportError:  # pragma: no cover
        pytest.skip(
            f"Không có {BIEN_MOI_TRUONG} và cũng không cài được testcontainers. "
            "Đặt biến đó trỏ tới PostgreSQL, hoặc cài nhóm dev đầy đủ."
        )

    with PostgresContainer(ANH_POSTGRES, dbname="xnk_test") as container:
        yield container.get_connection_url(driver=None)


@pytest.fixture(scope="session")
def settings(database_url: str) -> Settings:
    """Cấu hình trỏ vào database test.

    Dựng trực tiếp thay vì gọi ``Settings.tu_moi_truong()``: hàm đó đọc biến môi trường,
    và bộ test phải độc lập với môi trường của máy đang chạy.
    """
    return Settings(
        database_url=chuan_hoa_dsn(database_url),
        jwt_issuer=ISSUER_TEST,
        jwt_audience=AUDIENCE_TEST,
        jwt_signing_key=KHOA_KY_TEST,
    )


@pytest_asyncio.fixture(scope="session")
async def engine(settings: Settings) -> AsyncIterator[AsyncEngine]:
    """Engine trỏ vào database test, lược đồ đã được áp."""
    may = db.tao_engine(settings.database_url)
    await _ap_luoc_do(may)

    try:
        yield may
    finally:
        await may.dispose()


async def _ap_luoc_do(may: AsyncEngine) -> None:
    """Áp toàn bộ db/migrations/*.sql theo thứ tự tên file.

    Chạy qua **kết nối asyncpg gốc**, không qua ``conn.execute()`` của SQLAlchemy. Lý do:
    mỗi file migration chứa nhiều câu lệnh và cả khối ``DO $EF$…$EF$``, trong khi asyncpg
    mặc định gửi câu lệnh dưới dạng prepared statement — và giao thức đó chỉ nhận **một**
    câu lệnh mỗi lần, nên nó dừng với một lỗi cú pháp trỏ vào giữa file SQL hoàn toàn hợp lệ.

    ``Connection.execute()`` của asyncpg khi gọi không kèm tham số thì dùng giao thức truy
    vấn đơn giản, vốn chấp nhận nhiều câu lệnh — đúng thứ cần cho một file migration.
    """
    tep = sorted(THU_MUC_MIGRATION.glob("*.sql"))
    if not tep:
        raise RuntimeError(f"Không có migration nào trong {THU_MUC_MIGRATION}")

    async with may.connect() as conn:
        goc = await conn.get_raw_connection()
        driver = goc.driver_connection
        assert driver is not None
        for f in tep:
            # utf-8-sig: `dotnet ef migrations script` ghi file kèm BOM. `psql` bỏ qua BOM
            # nên không ai để ý, còn asyncpg gửi thẳng lên server và PostgreSQL dừng với
            # `syntax error at or near "﻿DO"` — một lỗi trỏ vào ký tự vô hình.
            await driver.execute(f.read_text(encoding="utf-8-sig"))


@pytest_asyncio.fixture
async def tenant_co_du_lieu(
    engine: AsyncEngine,
) -> AsyncIterator[tuple[uuid.UUID, uuid.UUID, str, str, str]]:
    """Dựng hai tenant, mỗi tenant một văn bản riêng, cộng một văn bản dùng chung.

    Trả về ``(tenant_a, tenant_b, số hiệu chung, số hiệu của A, số hiệu của B)``.

    Số hiệu sinh ngẫu nhiên mỗi lần chạy để nhiều lượt test không giẫm lên nhau trên một
    database dùng chung — đúng cách bộ test .NET làm.
    """
    tenant_a = uuid.uuid4()
    tenant_b = uuid.uuid4()
    so_chung = f"CHUNG-{uuid.uuid4().hex}"
    so_a = f"A-{uuid.uuid4().hex}"
    so_b = f"B-{uuid.uuid4().hex}"

    async with engine.begin() as conn:
        for so_hieu, tenant in ((so_chung, None), (so_a, tenant_a), (so_b, tenant_b)):
            await conn.execute(
                text(
                    'INSERT INTO corpus.documents ("Id", "TenantId", "DocumentNumber", "Title") '
                    "VALUES (:id, :tenant, :so_hieu, :tieu_de)"
                ),
                {
                    "id": uuid.uuid4(),
                    "tenant": tenant,
                    "so_hieu": so_hieu,
                    "tieu_de": f"Văn bản dùng cho test {so_hieu}",
                },
            )

    yield tenant_a, tenant_b, so_chung, so_a, so_b

    async with engine.begin() as conn:
        await conn.execute(
            text('DELETE FROM corpus.documents WHERE "DocumentNumber" = ANY(:ds)'),
            {"ds": [so_chung, so_a, so_b]},
        )
