#!/usr/bin/env python3
"""Tải file gốc của văn bản trong registry, tính sha256, ghi lock file.

Căn cứ: docs/00 §9.1 (nghĩa vụ khi thu thập) · docs/05 §4 (nghĩa vụ pháp lý) · story E2-03.

Bốn ràng buộc lấy thẳng từ tài liệu, cài đặt ở đây chứ không để người chạy tự nhớ:

  1. Tôn trọng robots.txt tuyệt đối          (docs/05 §4)
  2. Tối đa 1 request / 2 giây / domain      (docs/00 §9.1, docs/05 §4)
  3. User-Agent định danh kèm email liên hệ  (docs/00 §9.1, docs/05 §4)
  4. Idempotent, lưu file gốc bất biết kèm sha256 (docs/00 §9.1)

Mặc định là DRY-RUN. Muốn tải thật phải truyền --thuc-thi.

    python tools/corpus/thu_thap.py                 # xem sẽ tải gì
    python tools/corpus/thu_thap.py --thuc-thi      # tải thật
    python tools/corpus/thu_thap.py --ma 38-2015-TT-BTC --thuc-thi
"""

from __future__ import annotations

import argparse
import hashlib
import os
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import urllib.robotparser
from datetime import UTC, datetime
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
REGISTRY = GOC / "corpus" / "registry" / "van-ban.yaml"
LOCK = GOC / "corpus" / "registry" / "tai-ve.lock.yaml"
RAW = GOC / "corpus" / "raw"

# ── Định danh khi truy cập nguồn ────────────────────────────────────────────────────
# docs/00 §9.1 yêu cầu User-Agent ghi rõ tên tổ chức + email liên hệ. Đây là nghĩa vụ với
# chủ trang, không phải tuỳ chọn — nên script TỪ CHỐI CHẠY nếu chưa đặt.
# Đặt qua biến môi trường, không hardcode: XNK_CRAWL_UA và XNK_CRAWL_EMAIL.
UA_MAU = "rag-xnk/0.1 (+{email})"
DELAY_GIAY = 2.0
TIMEOUT_GIAY = 60
TRANG_THAI_DUOC_TAI = {"da_xac_minh", "da_tai", "da_doi_chieu"}


class ThuThapError(RuntimeError):
    pass


def lay_user_agent() -> str:
    email = os.environ.get("XNK_CRAWL_EMAIL", "").strip()
    if not email or "@" not in email:
        raise ThuThapError(
            "Chưa đặt XNK_CRAWL_EMAIL.\n"
            "docs/00 §9.1 và docs/05 §4 yêu cầu User-Agent định danh kèm email liên hệ.\n"
            "Đặt email liên hệ thật của tổ chức rồi chạy lại:\n"
            "    export XNK_CRAWL_EMAIL='ten@to-chuc.vn'"
        )
    return os.environ.get("XNK_CRAWL_UA", UA_MAU.format(email=email))


def doc_yaml(duong_dan: Path) -> dict:
    if not duong_dan.exists():
        raise ThuThapError(f"Không tìm thấy {duong_dan.relative_to(GOC)}")
    with duong_dan.open(encoding="utf-8") as f:
        return yaml.safe_load(f) or {}


def ten_file(ban_ghi: dict) -> str:
    """Quy ước đặt tên: {ma_van_ban}__{ngay_ban_hanh}.{duoi} — corpus/README.md §2.2."""
    ma = ban_ghi["ma_van_ban"]
    ngay = ban_ghi.get("ngay_ban_hanh")
    duoi = (ban_ghi.get("dinh_dang_file") or "pdf").lower().lstrip(".")
    if duoi not in {"pdf", "doc", "docx"}:
        duoi = "pdf"
    if ngay is None:
        return f"{ma}.{duoi}"
    return f"{ma}__{ngay}.{duoi}"


class HangDoiTheoDomain:
    """Giữ khoảng cách tối thiểu giữa hai request tới cùng một domain."""

    def __init__(self, delay: float = DELAY_GIAY) -> None:
        self._delay = delay
        self._lan_cuoi: dict[str, float] = {}

    def cho(self, domain: str) -> None:
        truoc = self._lan_cuoi.get(domain)
        if truoc is not None:
            con_lai = self._delay - (time.monotonic() - truoc)
            if con_lai > 0:
                time.sleep(con_lai)
        self._lan_cuoi[domain] = time.monotonic()


class KiemTraRobots:
    """Đọc và cache robots.txt cho từng domain. Không đọc được thì COI NHƯ CẤM."""

    def __init__(self, user_agent: str, hang_doi: HangDoiTheoDomain) -> None:
        self._ua = user_agent
        self._hang_doi = hang_doi
        self._cache: dict[str, urllib.robotparser.RobotFileParser | None] = {}

    def cho_phep(self, url: str) -> tuple[bool, str]:
        phan = urllib.parse.urlsplit(url)
        domain = phan.netloc
        if domain not in self._cache:
            self._hang_doi.cho(domain)
            self._cache[domain] = self._tai_robots(phan)
        rp = self._cache[domain]
        if rp is None:
            return False, "không đọc được robots.txt — mặc định là CẤM"
        if rp.can_fetch(self._ua, url):
            return True, "robots.txt cho phép"
        return False, "robots.txt cấm đường dẫn này"

    def _tai_robots(self, phan) -> urllib.robotparser.RobotFileParser | None:
        url = urllib.parse.urlunsplit((phan.scheme, phan.netloc, "/robots.txt", "", ""))
        rp = urllib.robotparser.RobotFileParser()
        req = urllib.request.Request(url, headers={"User-Agent": self._ua})
        try:
            with urllib.request.urlopen(req, timeout=TIMEOUT_GIAY) as resp:
                noi_dung = resp.read().decode("utf-8", errors="replace")
        except urllib.error.HTTPError as e:
            # 404 robots.txt nghĩa là không có luật cấm nào — đây là cách hiểu chuẩn.
            if e.code in (404, 410):
                rp.parse([])
                return rp
            return None
        except Exception:
            return None
        rp.parse(noi_dung.splitlines())
        return rp


def sha256_file(duong_dan: Path) -> str:
    h = hashlib.sha256()
    with duong_dan.open("rb") as f:
        for khoi in iter(lambda: f.read(1024 * 1024), b""):
            h.update(khoi)
    return h.hexdigest()


def tai_mot(url: str, dich: Path, user_agent: str) -> tuple[str, int]:
    req = urllib.request.Request(url, headers={"User-Agent": user_agent})
    tam = dich.with_suffix(dich.suffix + ".tam")
    with urllib.request.urlopen(req, timeout=TIMEOUT_GIAY) as resp, tam.open("wb") as f:
        while True:
            khoi = resp.read(1024 * 256)
            if not khoi:
                break
            f.write(khoi)
    tam.replace(dich)
    return sha256_file(dich), dich.stat().st_size


def chon_ban_ghi(registry: dict, ma_loc: str | None) -> tuple[list[dict], list[tuple[dict, str]]]:
    """Trả về (danh sách đủ điều kiện tải, danh sách bị bỏ qua kèm lý do)."""
    hop_le: list[dict] = []
    bo_qua: list[tuple[dict, str]] = []
    for ban_ghi in registry.get("van_ban") or []:
        ma = ban_ghi.get("ma_van_ban")
        if ma_loc and ma != ma_loc:
            continue
        if not ma:
            bo_qua.append((ban_ghi, "slot trống — chuyên gia chưa chốt văn bản"))
            continue
        trang_thai = ban_ghi.get("trang_thai_thu_thap")
        if trang_thai not in TRANG_THAI_DUOC_TAI:
            bo_qua.append(
                (ban_ghi, f"trang_thai_thu_thap='{trang_thai}' — cần 'da_xac_minh' trở lên")
            )
            continue
        if not ban_ghi.get("nguon_url"):
            bo_qua.append((ban_ghi, "chưa có nguon_url"))
            continue
        hop_le.append(ban_ghi)
    return hop_le, bo_qua


def main() -> int:
    ap = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    ap.add_argument("--thuc-thi", action="store_true", help="tải thật (mặc định là dry-run)")
    ap.add_argument("--ma", help="chỉ xử lý một ma_van_ban")
    args = ap.parse_args()

    registry = doc_yaml(REGISTRY)
    hop_le, bo_qua = chon_ban_ghi(registry, args.ma)

    print(f"Registry : {REGISTRY.relative_to(GOC)}")
    print(f"Chế độ   : {'THỰC THI' if args.thuc_thi else 'DRY-RUN (thêm --thuc-thi để tải thật)'}")
    print(f"Đủ điều kiện tải: {len(hop_le)} · bỏ qua: {len(bo_qua)}\n")

    if bo_qua:
        print("── Bỏ qua ─────────────────────────────────────────────────────────────")
        for ban_ghi, ly_do in bo_qua:
            nhan = ban_ghi.get("ma_van_ban") or f"[slot dòng {ban_ghi.get('dong_docs_04')}]"
            print(f"  {nhan:<24} {ly_do}")
        print()

    if not hop_le:
        print("Không có bản ghi nào đủ điều kiện tải.")
        print("Đây là hành vi đúng khi chuyên gia chưa xác minh — xem corpus/README.md §4.")
        return 0

    user_agent = lay_user_agent()
    hang_doi = HangDoiTheoDomain()
    robots = KiemTraRobots(user_agent, hang_doi)
    lock = doc_yaml(LOCK) if LOCK.exists() else {"phien_ban": 1, "file": {}}
    lock.setdefault("file", {})
    so_tai, so_bo, so_canh_bao = 0, 0, 0

    RAW.mkdir(parents=True, exist_ok=True)

    print("── Tải ────────────────────────────────────────────────────────────────")
    for ban_ghi in hop_le:
        ma = ban_ghi["ma_van_ban"]
        url = ban_ghi["nguon_url"]
        dich = RAW / ten_file(ban_ghi)
        da_ghi = lock["file"].get(ma)

        duoc, ly_do = robots.cho_phep(url)
        if not duoc:
            print(f"  ✗ {ma:<24} {ly_do}\n      {url}")
            so_bo += 1
            continue

        if dich.exists() and da_ghi:
            hien_tai = sha256_file(dich)
            if hien_tai == da_ghi.get("sha256"):
                print(f"  = {ma:<24} đã có, hash khớp — bỏ qua")
                so_bo += 1
                continue
            print(
                f"  ! {ma:<24} FILE ĐÃ ĐỔI so với hash đã ghi.\n"
                f"      đã ghi : {da_ghi.get('sha256')}\n"
                f"      hiện có: {hien_tai}\n"
                f"      File gốc phải bất biến (docs/00 §9.1). Điều tra trước khi ghi đè."
            )
            so_canh_bao += 1
            continue

        if not args.thuc_thi:
            print(f"  → {ma:<24} sẽ tải  {url}\n      ra: corpus/raw/{dich.name}")
            so_tai += 1
            continue

        hang_doi.cho(urllib.parse.urlsplit(url).netloc)
        try:
            digest, kich_thuoc = tai_mot(url, dich, user_agent)
        except Exception as e:
            print(f"  ✗ {ma:<24} lỗi tải: {e}")
            so_bo += 1
            continue

        lock["file"][ma] = {
            "ten_file": dich.name,
            "sha256": digest,
            "kich_thuoc_byte": kich_thuoc,
            "nguon_url": url,
            "tai_luc": datetime.now(UTC).isoformat(timespec="seconds"),
        }
        print(f"  ✓ {ma:<24} {kich_thuoc:>10,} byte  sha256={digest[:16]}…")
        so_tai += 1

    if args.thuc_thi and so_tai:
        lock["cap_nhat_lan_cuoi"] = datetime.now(UTC).date().isoformat()
        with LOCK.open("w", encoding="utf-8") as f:
            f.write(
                "# SINH TỰ ĐỘNG bởi tools/corpus/thu_thap.py — đừng sửa tay.\n"
                "# Bằng chứng toàn vẹn của file gốc (docs/00 §9.1: lưu file gốc bất biến kèm sha256).\n"
                "# File này NẰM TRONG GIT vì nó là bằng chứng, không phải nội dung corpus.\n\n"
            )
            yaml.safe_dump(lock, f, allow_unicode=True, sort_keys=True)
        print(f"\nĐã ghi {LOCK.relative_to(GOC)}")

    print(f"\nTổng: tải {so_tai} · bỏ qua {so_bo} · cảnh báo {so_canh_bao}")
    if so_canh_bao:
        print("⚠️  Có file đổi nội dung so với hash đã ghi — xử lý trước khi ingest.")
        return 2
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except ThuThapError as e:
        sys.exit(f"\n{e}")
