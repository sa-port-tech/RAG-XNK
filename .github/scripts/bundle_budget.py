#!/usr/bin/env python3
"""Đo kích thước gói Blazor WASM sau nén Brotli và đối chiếu với ngân sách.

Vì sao cần cổng này (docs/00 §6): người dùng thật là nhân viên tra cứu tại cảng và
kho, dùng điện thoại trên mạng di động. Gói WASM phình lên là một hồi quy về trải
nghiệm mà không test chức năng nào bắt được — nó chỉ hiện ra khi có người thật đứng
ở bãi container chờ trang tải.

Đo trên bản Brotli vì đó là thứ trình duyệt thật sự tải về. Đo trên file .dll chưa nén
sẽ cho một con số lớn gấp ba và vô nghĩa.

Dùng:
    python3 .github/scripts/bundle_budget.py \
        --dir artifacts/web/wwwroot/_framework --budget-kb 3500 --warn-at 85
"""

from __future__ import annotations

import argparse
import os
import sys
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        _stream.reconfigure(encoding="utf-8", errors="replace")

# Chỉ tính tài sản trình duyệt thật sự tải. Bản đồ debug và metadata không được gửi
# xuống client nên không tính vào ngân sách.
COUNTED_SUFFIXES = {".wasm", ".dll", ".js", ".dat", ".blat", ".json"}
IGNORED_NAMES = {"blazor.boot.json.br", "segments"}
IGNORED_SUFFIXES = {".pdb", ".map", ".gz"}


def brotli_size(path: Path) -> int:
    """Kích thước sau nén Brotli, ưu tiên file .br có sẵn từ bước publish."""
    sibling = path.with_suffix(path.suffix + ".br")
    if sibling.is_file():
        return sibling.stat().st_size

    try:
        import brotli
    except ModuleNotFoundError:
        sys.exit(
            "bundle_budget.py: không tìm thấy file .br cạnh tài sản và chưa cài module "
            "`brotli`. Thêm `pip install brotli` vào workflow, hoặc bật nén khi publish."
        )

    # quality=11 là mức publish của Blazor dùng cho tài sản tĩnh.
    return len(brotli.compress(path.read_bytes(), quality=11))


def summary(markdown: str) -> None:
    target = os.environ.get("GITHUB_STEP_SUMMARY")
    if target:
        with open(target, "a", encoding="utf-8") as handle:
            handle.write(markdown + "\n")


def vi(value: float, digits: int = 1) -> str:
    return f"{value:.{digits}f}".replace(".", ",")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dir", required=True, help="Thư mục _framework của bản publish")
    parser.add_argument("--budget-kb", type=float, required=True)
    parser.add_argument("--warn-at", type=float, default=85.0, help="Cảnh báo khi dùng quá % ngân sách")
    parser.add_argument("--top", type=int, default=15, help="Số tài sản lớn nhất cần liệt kê")
    args = parser.parse_args()

    root = Path(args.dir)
    if not root.is_dir():
        print(f"::error::Không tìm thấy thư mục '{root}'. Bước publish Blazor đã chạy chưa?")
        sys.exit(1)

    sizes: list[tuple[int, Path]] = []
    for path in sorted(root.rglob("*")):
        if not path.is_file():
            continue
        if path.suffix in IGNORED_SUFFIXES or path.name in IGNORED_NAMES:
            continue
        if path.suffix == ".br":
            continue  # tính qua tài sản gốc, tránh đếm hai lần
        if path.suffix not in COUNTED_SUFFIXES:
            continue
        sizes.append((brotli_size(path), path))

    if not sizes:
        print(f"::error::Không có tài sản nào đo được trong '{root}'.")
        sys.exit(1)

    total_kb = sum(size for size, _ in sizes) / 1024.0
    used_percent = total_kb / args.budget_kb * 100.0
    over = total_kb > args.budget_kb

    sizes.sort(reverse=True)
    rows = "\n".join(
        f"| `{path.relative_to(root).as_posix()}` | {vi(size / 1024.0)} KB |"
        for size, path in sizes[: args.top]
    )

    icon = "❌" if over else ("⚠️" if used_percent >= args.warn_at else "✅")
    summary(
        f"### {icon} Ngân sách gói Blazor WASM\n\n"
        f"| Chỉ số | Giá trị |\n|---|---|\n"
        f"| Tổng sau nén Brotli | **{vi(total_kb)} KB** |\n"
        f"| Ngân sách | {vi(args.budget_kb)} KB |\n"
        f"| Đã dùng | {vi(used_percent)}% |\n"
        f"| Số tài sản | {len(sizes)} |\n\n"
        f"<details><summary>{args.top} tài sản lớn nhất</summary>\n\n"
        f"| Tài sản | Brotli |\n|---|---|\n{rows}\n\n</details>\n"
    )

    print(f"Tổng Brotli: {vi(total_kb)} KB / ngân sách {vi(args.budget_kb)} KB ({vi(used_percent)}%)")

    if over:
        print(
            f"::error::Gói WASM {vi(total_kb)} KB vượt ngân sách {vi(args.budget_kb)} KB "
            f"khai báo trong .github/quality-gates.yml. Xử lý bằng trimming, lazy loading "
            f"assembly, hoặc bỏ bớt phụ thuộc — không nới ngân sách để cho qua."
        )
        sys.exit(1)

    if used_percent >= args.warn_at:
        print(
            f"::warning::Đã dùng {vi(used_percent)}% ngân sách gói WASM. "
            f"Còn ít dư địa cho các tính năng sắp tới."
        )


if __name__ == "__main__":
    main()
