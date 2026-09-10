#!/usr/bin/env python3
"""Đối chiếu registry với checklist hoàn thành của docs/04 §7.

Trả lời đúng một câu hỏi: **Sprint 1 đã có căn cứ để bắt đầu chưa?**

docs/04 §7 kết thúc bằng "Chỉ khi checklist đủ, Sprint 1 mới có căn cứ bắt đầu."
Script này biến checklist đó thành thứ chạy được, để trạng thái là dữ kiện chứ không phải
cảm nhận trong buổi standup.

    python tools/corpus/kiem_tra.py
    python tools/corpus/kiem_tra.py --chi-tiet

Mã thoát: 0 = đủ điều kiện · 1 = còn hạng mục chưa đạt.
"""

from __future__ import annotations

import argparse
import sys
import unicodedata
from pathlib import Path

try:
    import yaml
except ImportError:  # pragma: no cover
    sys.exit("Thiếu PyYAML. Cài bằng: pip install pyyaml")

# Console Windows mặc định cp1252, không in được tiếng Việt. Ép UTF-8 để báo cáo đọc được
# thay vì ném UnicodeEncodeError giữa chừng.
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

GOC = Path(__file__).resolve().parents[2]
THU_MUC = GOC / "corpus" / "registry"
REGISTRY = THU_MUC / "van-ban.yaml"
QUAN_HE = THU_MUC / "quan-he-sua-doi.yaml"
CO_QUAN = THU_MUC / "co-quan-ban-hanh.yaml"
LOCK = THU_MUC / "tai-ve.lock.yaml"

# Chữ ký của chuyên gia XNK cho hạng mục ⑧. File này KHÔNG có sẵn trong repo — nó xuất
# hiện khi có người thật ký, và biến mất khỏi ý nghĩa nếu ai đó tạo nó hộ.
CHU_KY = THU_MUC / "chu-ky-chuyen-gia.yaml"

# docs/00 §9.4 — enum trạng thái hiệu lực. Registry chỉ được dùng đúng các giá trị này.
TRANG_THAI_HOP_LE = {
    "chua_co_hieu_luc",
    "con_hieu_luc",
    "het_hieu_luc_mot_phan",
    "het_hieu_luc",
    "ngung_hieu_luc",
}

# docs/04 §3 — cột "Bắt buộc" đánh ✅
BAT_BUOC = [
    "so_hieu",
    "loai_van_ban",
    "trich_yeu",
    "co_quan_ban_hanh",
    "ngay_ban_hanh",
    "ngay_hieu_luc",
    "trang_thai",
    "co_vbhn",
    "nguon_url",
    "dinh_dang_file",
    "so_dieu_uoc_tinh",
]


class KetQua:
    def __init__(self) -> None:
        self.muc: list[tuple[bool, str, list[str]]] = []

    def them(self, dat: bool, nhan: str, chi_tiet: list[str] | None = None) -> None:
        self.muc.append((dat, nhan, chi_tiet or []))

    @property
    def dat_het(self) -> bool:
        return all(dat for dat, _, _ in self.muc)


def doc(duong_dan: Path) -> dict:
    if not duong_dan.exists():
        return {}
    with duong_dan.open(encoding="utf-8") as f:
        return yaml.safe_load(f) or {}


def slug(so_hieu: str) -> str:
    """so_hieu → ma_van_ban, theo corpus/README.md §2.1.

    Gấp về ASCII có chủ đích. `ma_van_ban` là ĐỊNH DANH — nó đi vào tên file, S3 key, log
    và URL; ký tự `Đ` trong `NĐ-CP`/`QĐ-TTg` gây phiền ở cả ba chỗ đó. `so_hieu` mới là
    giá trị hiển thị và giữ nguyên dấu, và đó mới là thứ dùng để trích dẫn.

    Ghi chú cho E3-09: đây cũng là dạng canonical mà bộ chuẩn hoá số hiệu phải sinh ra,
    vì người dùng gõ cả "NĐ 08/2015" lẫn "ND 08/2015".
    """
    thay = so_hieu.replace("Đ", "D").replace("đ", "d")
    khong_dau = "".join(
        c for c in unicodedata.normalize("NFD", thay) if not unicodedata.combining(c)
    )
    return khong_dau.replace("/", "-").upper()


def da_chot(bg: dict) -> bool:
    """Bản ghi đã được chốt văn bản (khác slot trống)."""
    return bool(bg.get("ma_van_ban") and bg.get("so_hieu"))


def kiem_tra() -> KetQua:
    kq = KetQua()
    reg = doc(REGISTRY)
    ban_ghi = reg.get("van_ban") or []
    tieu_chi = reg.get("tieu_chi") or {}
    da_chot_list = [b for b in ban_ghi if da_chot(b)]

    # ── Toàn vẹn cấu trúc (không nằm trong docs/04 §7 nhưng sai thì mọi thứ dưới vô nghĩa)
    loi_cau_truc: list[str] = []
    thay = {}
    for b in da_chot_list:
        ma, so_hieu = b["ma_van_ban"], b["so_hieu"]
        if ma != slug(so_hieu):
            loi_cau_truc.append(
                f"{ma}: không khớp quy ước — từ '{so_hieu}' phải ra '{slug(so_hieu)}'"
            )
        if ma in thay:
            loi_cau_truc.append(f"{ma}: trùng ma_van_ban")
        thay[ma] = b
        tt = b.get("trang_thai")
        if tt is not None and tt not in TRANG_THAI_HOP_LE:
            loi_cau_truc.append(f"{ma}: trang_thai='{tt}' không thuộc enum docs/00 §9.4")
    kq.them(not loi_cau_truc, "Toàn vẹn cấu trúc registry", loi_cau_truc)

    # ── §7 ①  15 dòng có số hiệu chính xác, xác minh trên vbpl.vn
    muc_tieu = tieu_chi.get("so_van_ban_muc_tieu", 15)
    thieu = muc_tieu - len(da_chot_list)
    chua_xac_minh = [
        b["ma_van_ban"]
        for b in da_chot_list
        if not (b.get("xac_minh_vbpl") or {}).get("da_xac_minh")
    ]
    kq.them(
        thieu <= 0 and not chua_xac_minh,
        f"① Đủ {muc_tieu} văn bản, đã xác minh trên vbpl.vn",
        (
            ([f"còn thiếu {thieu} văn bản chưa chốt"] if thieu > 0 else [])
            + ([f"chưa xác minh vbpl.vn: {', '.join(chua_xac_minh)}"] if chua_xac_minh else [])
        ),
    )

    # ── §7 ②  Metadata bắt buộc theo §3
    thieu_meta: list[str] = []
    for b in da_chot_list:
        trong = [t for t in BAT_BUOC if b.get(t) in (None, "", [])]
        if trong:
            thieu_meta.append(f"{b['ma_van_ban']}: thiếu {', '.join(trong)}")
    kq.them(not thieu_meta, "② Mỗi văn bản có đủ metadata bắt buộc (§3)", thieu_meta)

    # ── §7 ③  ≥2 cặp sửa đổi đã liệt kê chi tiết Điều/Khoản
    qh = doc(QUAN_HE).get("quan_he") or []
    day_du = [
        q
        for q in qh
        if q.get("van_ban_sua_doi")
        and q.get("van_ban_bi_sua_doi")
        and q.get("loai")
        and q.get("pham_vi_anh_huong")
    ]
    can = tieu_chi.get("so_cap_sua_doi_toi_thieu", 2)
    thieu_ph = [
        f"{q.get('id')}: {q.get('van_ban_sua_doi') or '?'} → {q.get('van_ban_bi_sua_doi') or '?'}"
        f" — {'pham_vi_anh_huong rỗng' if not q.get('pham_vi_anh_huong') else 'thiếu loai'}"
        for q in qh
        if q not in day_du and (q.get("van_ban_sua_doi") or q.get("van_ban_bi_sua_doi"))
    ]
    kq.them(
        len(day_du) >= can,
        f"③ ≥{can} cặp sửa đổi liệt kê chi tiết tới cấp Điều/Khoản",
        ([f"mới có {len(day_du)}/{can} cặp đầy đủ"] if len(day_du) < can else []) + thieu_ph,
    )

    # ── §7 ④  ≥1 công văn hướng dẫn (điều kiện để test guardrail ④ / BR-06)
    cong_van = [b for b in da_chot_list if b.get("vai_tro") == "cong_van"]
    can_cv = tieu_chi.get("so_cong_van_toi_thieu", 1)
    kq.them(
        len(cong_van) >= can_cv,
        f"④ ≥{can_cv} công văn hướng dẫn trong corpus",
        (
            [
                f"mới có {len(cong_van)}/{can_cv} — không có công văn thì BR-06/guardrail ④ "
                "không có dữ liệu để kiểm chứng (docs/04 §2)"
            ]
            if len(cong_van) < can_cv
            else []
        ),
    )

    # ── §7 ⑤  Không quá 3 văn bản dạng PDF scan
    scan = [b["ma_van_ban"] for b in da_chot_list if (b.get("dinh_dang_file") or "") == "pdf_scan"]
    toi_da = tieu_chi.get("so_van_ban_scan_toi_da", 3)
    kq.them(
        len(scan) <= toi_da,
        f"⑤ Không quá {toi_da} văn bản dạng PDF scan",
        ([f"đang có {len(scan)}: {', '.join(scan)}"] if len(scan) > toi_da else []),
    )

    # ── §7 ⑥  Bảng ánh xạ cơ quan ban hành đã điền
    anh_xa = [
        a
        for a in (doc(CO_QUAN).get("anh_xa") or [])
        if a.get("ten_tai_thoi_diem_ban_hanh") and a.get("ten_hien_hanh")
    ]
    kq.them(
        bool(anh_xa),
        "⑥ Bảng ánh xạ cơ quan ban hành (§6) đã điền",
        (["chưa có dòng nào — docs/00 §1.2 cảnh báo tái cơ cấu bộ máy 2025"] if not anh_xa else []),
    )

    # ── §7 ⑦  File gốc đã tải, có sha256
    lock = (doc(LOCK).get("file") or {}) if LOCK.exists() else {}
    chua_tai = [b["ma_van_ban"] for b in da_chot_list if b["ma_van_ban"] not in lock]
    kq.them(
        bool(da_chot_list) and not chua_tai,
        "⑦ File gốc đã tải về, có sha256",
        ([f"chưa tải: {', '.join(chua_tai)}"] if chua_tai else []),
    )

    # ── §7 ⑧  Chuyên gia ký xác nhận
    #
    # Trước đây hạng mục này là hằng `False`. Hệ quả số học: `dat_het` không bao giờ đúng,
    # nên `main()` không bao giờ tới được `return 0` — trong khi docstring đầu file quảng
    # cáo "Mã thoát: 0 = đủ điều kiện". Một script chỉ có một kết cục thì nó không trả lời
    # câu hỏi nào cả; nó chỉ in ra một bảng.
    #
    # Nay nó đọc một file chữ ký có thật. File đó KHÔNG nằm sẵn trong repo và script này
    # cũng không tạo nó: chữ ký do script sinh ra thì không phải chữ ký. Người ký commit
    # nó, và commit ấy là dấu vết truy được.
    kq.them(*_kiem_chu_ky())

    return kq


def _kiem_chu_ky() -> tuple[bool, str, list[str]]:
    """Đọc chữ ký chuyên gia cho hạng mục ⑧ của docs/04 §7."""
    nhan = "⑧ Chuyên gia XNK ký xác nhận (docs/04 §8)"

    if not CHU_KY.is_file():
        return (
            False,
            nhan,
            [
                f"chưa có {CHU_KY.relative_to(GOC)}",
                "chuyên gia ký bằng cách commit file đó với: nguoi_ky, ngay_ky, "
                "pham_vi (danh sách ma_van_ban đã xác minh)",
            ],
        )

    try:
        noi_dung = yaml.safe_load(CHU_KY.read_text("utf-8")) or {}
    except yaml.YAMLError as loi:
        return (False, nhan, [f"{CHU_KY.name} không đọc được: {loi}"])

    thieu = [k for k in ("nguoi_ky", "ngay_ky", "pham_vi") if not noi_dung.get(k)]
    if thieu:
        return (False, nhan, [f"{CHU_KY.name} thiếu trường: {', '.join(thieu)}"])

    return (
        True,
        nhan,
        [
            f"{noi_dung['nguoi_ky']} ký ngày {noi_dung['ngay_ky']}, "
            f"{len(noi_dung['pham_vi'])} văn bản"
        ],
    )


def main() -> int:
    ap = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    ap.add_argument("--chi-tiet", action="store_true", help="in đầy đủ chi tiết từng hạng mục")
    args = ap.parse_args()

    kq = kiem_tra()
    print("Checklist docs/04 §7 — điều kiện gỡ chặn Sprint 1")
    print("═" * 72)
    for dat, nhan, chi_tiet in kq.muc:
        print(f"  [{'x' if dat else ' '}] {nhan}")
        for dong in chi_tiet if args.chi_tiet else chi_tiet[:3]:
            print(f"        · {dong}")
        if not args.chi_tiet and len(chi_tiet) > 3:
            print(f"        · … còn {len(chi_tiet) - 3} mục (chạy với --chi-tiet)")
    print("═" * 72)

    so_dat = sum(1 for dat, _, _ in kq.muc if dat)
    print(f"Đạt {so_dat}/{len(kq.muc)} hạng mục.")
    if kq.dat_het:
        print("✅ Registry đủ điều kiện — Sprint 1 có căn cứ bắt đầu.")
        return 0
    print("⛔ Chưa đủ. docs/04 §7: 'Chỉ khi checklist đủ, Sprint 1 mới có căn cứ bắt đầu.'")
    return 1


if __name__ == "__main__":
    sys.exit(main())
