#!/usr/bin/env python3
"""Sinh script seed cho máy local từ `corpus/registry/van-ban.yaml`.

Vì sao seed được SINH chứ không gõ tay
---------------------------------------
Danh mục 15 văn bản lõi là dữ liệu thật, có số hiệu và nguồn tra ghi trong registry
(`docs/04`). Chép tay sang SQL là tạo ra bản sao thứ hai của cùng một sự thật, và bản sao
thứ hai luôn là bản sẽ cũ đi. Sinh lại từ registry thì chỉ có một nguồn.

Cùng lý do mà `db/migrations/*.sql` được **xuất** từ EF Core chứ không viết tay (ADR-010):
file `.sql` là hợp đồng đem đi dùng, còn nguồn sự thật nằm chỗ khác.

Ranh giới không được vượt (D6 trong `docs/19`)
----------------------------------------------
* Chỉ seed **metadata**: số hiệu và trích yếu. Không có nội dung văn bản — corpus không
  nằm trong git (ADR-008).
* `ngay_hieu_luc` / `ngay_het_hieu_luc` trong registry đang là ``null`` vì **chuyên gia
  chưa xác minh trên vbpl.vn**. Seed giữ nguyên ``NULL``, không đoán. Điền bừa vào đây là
  đúng thứ mà `corpus/registry/van-ban.yaml` mở đầu bằng lời cảnh báo cấm làm.
* Bốn SOP nội bộ ở file thứ hai là **dữ liệu mẫu cho máy local**, không phải văn bản pháp
  luật. Chúng mang tiền tố ``[MẪU LOCAL]`` ngay trong trích yếu để không ai nhầm.

Dùng
----
    python tools/local/sinh_seed.py            # ghi vào db/seed/
    python tools/local/sinh_seed.py --kiem-tra # chỉ báo file đã cũ so với registry
"""

from __future__ import annotations

import argparse
import sys
import uuid
from pathlib import Path
from typing import Any

try:
    import yaml
except ImportError:  # pragma: no cover
    sys.exit("Thiếu PyYAML. Cài bằng: pip install pyyaml")

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

GOC = Path(__file__).resolve().parents[2]
REGISTRY = GOC / "corpus" / "registry" / "van-ban.yaml"
THU_MUC_SEED = GOC / "db" / "seed"

# Không gian tên để sinh khoá chính ổn định.
#
# Khoá chính phải là cùng một giá trị ở mọi máy và qua mọi lần chạy lại, nếu không thì
# `ON CONFLICT DO NOTHING` vô dụng và mỗi lần bootstrap lại nhân đôi dữ liệu. UUIDv5 cho
# đúng tính chất đó mà không cần ai phải giữ một bảng ánh xạ.
#
# Bản thân không gian tên cũng được suy ra chứ không phải một chuỗi hex ai đó gõ đại.
NS_DU_AN = uuid.uuid5(uuid.NAMESPACE_URL, "https://github.com/sa-port-tech/RAG-XNK")
NS_VAN_BAN = uuid.uuid5(NS_DU_AN, "corpus.documents")
NS_TENANT = uuid.uuid5(NS_DU_AN, "identity.tenants")

# Hai tenant dùng cho máy local. `loai` lấy đúng enum của TenantClaims.TenantType
# (`noi_bo` | `b2b_khach_hang` | `dao_tao`) — xem src/dotnet/Xnk.Shared/Tenancy/TenantClaims.cs.
#
# ⚠️ Story E1-11 phần `identity-tenant` seed đúng hai slug này vào `identity.tenants`.
# Đổi slug ở đây mà quên đổi bên đó thì văn bản riêng của tenant sẽ mồ côi — không có
# khoá ngoại nào bắt được, vì hai schema thuộc hai service khác nhau và `docs/00` §4.4
# cấm JOIN chéo schema.
TENANT_LOCAL = [
    ("noi-bo", "Phòng XNK — nội bộ", "noi_bo"),
    ("b2b-khach-hang", "Công ty Giao nhận Cảng Xanh", "b2b_khach_hang"),
]

# SOP nội bộ dùng cho máy local. Tồn tại vì bộ lọc cách ly tenant chỉ chứng minh được điều
# gì khi có dữ liệu RIÊNG của tenant để mà lọc — 15 văn bản quy phạm pháp luật đều dùng
# chung (`TenantId IS NULL`) nên tự chúng không phân biệt được hai tenant.
SOP_LOCAL = [
    ("noi-bo", "SOP-NB-01", "[MẪU LOCAL] Quy trình nội bộ: kiểm tra bộ chứng từ trước khi mở tờ khai"),
    ("noi-bo", "SOP-NB-02", "[MẪU LOCAL] Quy trình nội bộ: xử lý tờ khai luồng đỏ"),
    ("b2b-khach-hang", "SOP-KH-01", "[MẪU LOCAL] Hướng dẫn khách hàng: chuẩn bị hồ sơ nhập khẩu hàng bách hoá"),
    ("b2b-khach-hang", "SOP-KH-02", "[MẪU LOCAL] Hướng dẫn khách hàng: khai báo trị giá hải quan"),
]

# Giới hạn cột trong `corpus.documents` (xem Xnk.Corpus/Data/CorpusDbContext.cs).
# Vượt giới hạn thì dừng hẳn: cắt bớt âm thầm là cách biến một số hiệu sai thành dữ liệu
# trông vẫn hợp lệ.
DAI_TOI_DA_SO_HIEU = 100
DAI_TOI_DA_TRICH_YEU = 1000


def ma_tenant(slug: str) -> uuid.UUID:
    """Khoá chính ổn định của một tenant, suy ra từ slug."""
    return uuid.uuid5(NS_TENANT, slug)


def ma_van_ban(ma: str) -> uuid.UUID:
    """Khoá chính ổn định của một văn bản, suy ra từ `ma_van_ban` trong registry."""
    return uuid.uuid5(NS_VAN_BAN, ma)


def _thoat_chuoi(gia_tri: str) -> str:
    """Thoát dấu nháy đơn cho chuỗi SQL."""
    return gia_tri.replace("'", "''")


def _doc_registry() -> list[dict[str, Any]]:
    if not REGISTRY.is_file():
        sys.exit(f"✗ Không tìm thấy registry: {REGISTRY}")

    du_lieu = yaml.safe_load(REGISTRY.read_text(encoding="utf-8"))
    ban_ghi = du_lieu.get("van_ban") or []
    if not ban_ghi:
        sys.exit("✗ Registry không có bản ghi văn bản nào.")
    return ban_ghi


def _ngay_sql(gia_tri: Any) -> str:
    """Đổi một ngày trong registry thành literal SQL, giữ nguyên ``NULL`` khi chưa xác minh."""
    if gia_tri is None:
        return "NULL"
    return f"DATE '{gia_tri}'"


def sinh_van_ban_chung(ban_ghi: list[dict[str, Any]]) -> tuple[str, int, int]:
    """Sinh seed cho các văn bản đã xác định được số hiệu.

    Trả về ``(nội dung SQL, số bản ghi đưa vào, số slot bỏ qua)``.

    Registry chứa hai loại bản ghi. Loại thứ nhất có ``so_hieu`` thật, tra được từ nguồn
    chính thức. Loại thứ hai là **slot**: BA biết cần một văn bản ở vị trí đó nhưng chưa
    xác định được là văn bản nào, nên chỉ có ``mo_ta_slot``. Slot bị **bỏ qua**, và số
    lượng bỏ qua được in ra — bịa một số hiệu để lấp chỗ trống là đúng thứ mà lời cảnh báo
    ở đầu `corpus/registry/van-ban.yaml` cấm làm.
    """
    dong: list[str] = []
    bo_qua = 0

    for muc in ban_ghi:
        ma = muc.get("ma_van_ban")
        so_hieu = muc.get("so_hieu")
        trich_yeu = muc.get("trich_yeu")

        if so_hieu is None:
            bo_qua += 1
            continue

        # Có số hiệu nhưng thiếu hai trường kia là registry mâu thuẫn với chính nó, không
        # phải slot. Dừng lại để người sửa registry, đừng lặng lẽ bỏ bản ghi thật.
        if not ma or not trich_yeu:
            sys.exit(f"✗ Bản ghi '{so_hieu}' có số hiệu nhưng thiếu ma_van_ban hoặc trich_yeu.")

        if len(so_hieu) > DAI_TOI_DA_SO_HIEU:
            sys.exit(f"✗ so_hieu '{so_hieu}' dài quá {DAI_TOI_DA_SO_HIEU} ký tự.")
        if len(trich_yeu) > DAI_TOI_DA_TRICH_YEU:
            sys.exit(f"✗ trich_yeu của '{ma}' dài quá {DAI_TOI_DA_TRICH_YEU} ký tự.")

        dong.append(
            f"    ('{ma_van_ban(ma)}', NULL, '{_thoat_chuoi(so_hieu)}', "
            f"'{_thoat_chuoi(trich_yeu)}', "
            f"{_ngay_sql(muc.get('ngay_hieu_luc'))}, {_ngay_sql(muc.get('ngay_het_hieu_luc'))})"
        )

    if not dong:
        sys.exit("✗ Không có bản ghi nào trong registry đã xác định được số hiệu.")

    than = ",\n".join(dong)

    noi_dung = f"""-- ⚠️ FILE SINH TỰ ĐỘNG — sinh lại bằng: python tools/local/sinh_seed.py
--
-- Nguồn: corpus/registry/van-ban.yaml — {len(ban_ghi)} bản ghi, đưa vào {len(dong)},
-- bỏ qua {bo_qua} slot chưa xác định được số hiệu (chỉ có mo_ta_slot).
-- Sửa registry rồi sinh lại; đừng sửa file này.
--
-- Đây là văn bản quy phạm pháp luật DÙNG CHUNG cho mọi tenant, nên `TenantId` là NULL —
-- ngữ nghĩa của bộ lọc trong CorpusDbContext.OnModelCreating (ADR-012).
--
-- `EffectiveFrom` / `EffectiveTo` để NULL vì chuyên gia XNK chưa xác minh hiệu lực trên
-- vbpl.vn. Đó là dữ kiện chưa có, không phải ô cần điền cho đẹp.
--
-- Chạy lại nhiều lần an toàn nhờ khoá chính ổn định (UUIDv5) + ON CONFLICT DO NOTHING.

INSERT INTO corpus.documents
    ("Id", "TenantId", "DocumentNumber", "Title", "EffectiveFrom", "EffectiveTo")
VALUES
{than}
ON CONFLICT ("Id") DO NOTHING;
"""
    return noi_dung, len(dong), bo_qua


def sinh_sop_tenant() -> str:
    dong: list[str] = []
    for slug, so_hieu, trich_yeu in SOP_LOCAL:
        khoa = uuid.uuid5(NS_VAN_BAN, f"sop-local:{slug}:{so_hieu}")
        dong.append(
            f"    ('{khoa}', '{ma_tenant(slug)}', '{_thoat_chuoi(so_hieu)}', "
            f"'{_thoat_chuoi(trich_yeu)}', NULL, NULL)"
        )

    than = ",\n".join(dong)
    bang_tenant = "\n".join(
        f"--   {slug:<16} {ma_tenant(slug)}  ({loai})" for slug, _, loai in TENANT_LOCAL
    )

    return f"""-- ⚠️ FILE SINH TỰ ĐỘNG — sinh lại bằng: python tools/local/sinh_seed.py
--
-- SOP nội bộ của từng tenant, DÙNG CHO MÁY LOCAL. Đây KHÔNG phải văn bản pháp luật;
-- trích yếu mang tiền tố [MẪU LOCAL] để không ai nhầm khi nhìn thấy trong giao diện.
--
-- Vì sao cần: bộ lọc cách ly tenant chỉ chứng minh được điều gì khi có dữ liệu RIÊNG của
-- tenant. Toàn bộ 16 văn bản ở seed 0001 đều dùng chung (TenantId IS NULL), nên tự chúng
-- không phân biệt được hai tenant.
--
-- Khoá tenant (suy ra bằng UUIDv5 từ slug, xem tools/local/sinh_seed.py):
{bang_tenant}
--
-- Không có khoá ngoại sang `identity.tenants`: hai schema thuộc hai service khác nhau và
-- docs/00 §4.4 cấm ràng buộc chéo schema. Ràng buộc đó nằm ở tầng nghiệp vụ.

INSERT INTO corpus.documents
    ("Id", "TenantId", "DocumentNumber", "Title", "EffectiveFrom", "EffectiveTo")
VALUES
{than}
ON CONFLICT ("Id") DO NOTHING;
"""


def main() -> int:
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument(
        "--kiem-tra",
        action="store_true",
        help="Không ghi gì, chỉ báo file seed đã lệch so với registry (mã thoát 1)",
    )
    tham_so = parser.parse_args()

    ban_ghi = _doc_registry()
    sql_chung, so_dua_vao, so_bo_qua = sinh_van_ban_chung(ban_ghi)
    ket_qua = {
        THU_MUC_SEED / "0001_corpus_van_ban_chung.sql": sql_chung,
        THU_MUC_SEED / "0002_corpus_sop_tenant.sql": sinh_sop_tenant(),
    }

    print(
        f"Registry: {len(ban_ghi)} bản ghi → {so_dua_vao} văn bản có số hiệu, "
        f"{so_bo_qua} slot bỏ qua."
    )

    lech = False
    for duong_dan, noi_dung in ket_qua.items():
        hien_tai = duong_dan.read_text(encoding="utf-8") if duong_dan.is_file() else None
        if hien_tai == noi_dung:
            print(f"= {duong_dan.relative_to(GOC)} đã khớp registry")
            continue

        lech = True
        if tham_so.kiem_tra:
            print(f"✗ {duong_dan.relative_to(GOC)} lệch so với registry")
            continue

        duong_dan.parent.mkdir(parents=True, exist_ok=True)
        duong_dan.write_text(noi_dung, encoding="utf-8", newline="\n")
        print(f"✓ Đã ghi {duong_dan.relative_to(GOC)}")

    if tham_so.kiem_tra and lech:
        print("\nChạy `python tools/local/sinh_seed.py` để sinh lại.")
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
