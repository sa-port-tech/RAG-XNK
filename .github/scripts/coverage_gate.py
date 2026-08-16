#!/usr/bin/env python3
"""Gộp báo cáo coverage định dạng Cobertura và đối chiếu với ngưỡng tối thiểu.

Cả .NET (`dotnet test --collect:"XPlat Code Coverage"`, qua coverlet.collector) lẫn
Python (`pytest --cov --cov-report=xml`) đều xuất Cobertura, nên một script phục vụ
được cả hai stack.

Coverage được tính lại từ số dòng thật (`lines-valid` / `lines-covered` cộng dồn trên
mọi file) chứ không lấy trung bình cộng thuộc tính `line-rate` của từng report — trung
bình cộng của các tỉ lệ là một con số vô nghĩa khi các assembly có kích thước khác nhau.

Dùng:
    python3 .github/scripts/coverage_gate.py --min 70 'artifacts/**/coverage.cobertura.xml'
"""

from __future__ import annotations

import argparse
import glob
import os
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        _stream.reconfigure(encoding="utf-8", errors="replace")


def collect(patterns: list[str]) -> list[Path]:
    found: list[Path] = []
    for pattern in patterns:
        found.extend(Path(p) for p in glob.glob(pattern, recursive=True))
    return sorted(set(found))


def count_lines(report: Path) -> tuple[int, int]:
    """Trả về (số dòng đã phủ, tổng số dòng có thể phủ) của một file Cobertura."""
    root = ET.parse(report).getroot()

    covered = 0
    total = 0
    # Đếm ở cấp <line> để không phụ thuộc vào việc công cụ có điền lines-valid hay không,
    # và để tránh đếm trùng khi một class xuất hiện ở nhiều package.
    seen: set[tuple[str, str]] = set()
    for cls in root.iter("class"):
        filename = cls.get("filename", "")
        for line in cls.iter("line"):
            number = line.get("number", "")
            key = (filename, number)
            if key in seen:
                continue
            seen.add(key)
            total += 1
            if int(line.get("hits", "0")) > 0:
                covered += 1

    return covered, total


def emit(name: str, value: str) -> None:
    """Ghi output cho GitHub Actions nếu đang chạy trong Actions."""
    target = os.environ.get("GITHUB_OUTPUT")
    if target:
        with open(target, "a", encoding="utf-8") as handle:
            handle.write(f"{name}={value}\n")


def summary(markdown: str) -> None:
    target = os.environ.get("GITHUB_STEP_SUMMARY")
    if target:
        with open(target, "a", encoding="utf-8") as handle:
            handle.write(markdown + "\n")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("patterns", nargs="+", help="Glob tới các file cobertura.xml")
    parser.add_argument("--min", type=float, required=True, help="Ngưỡng phần trăm tối thiểu")
    parser.add_argument("--label", default="coverage", help="Nhãn hiển thị, ví dụ dotnet")
    args = parser.parse_args()

    reports = collect(args.patterns)
    if not reports:
        # Không có report là một sự cố cấu hình, không phải "coverage bằng 0".
        # Im lặng cho qua ở đây nghĩa là cổng coverage bị vô hiệu hoá mà không ai biết.
        print(f"::error::Không tìm thấy báo cáo coverage nào khớp {args.patterns}.")
        print(
            "::error::Kiểm tra project test đã tham chiếu coverlet.collector (.NET) "
            "hoặc đã bật pytest-cov (Python) chưa."
        )
        sys.exit(1)

    covered = 0
    total = 0
    for report in reports:
        c, t = count_lines(report)
        covered += c
        total += t
        print(f"  {report}: {c}/{t}")

    if total == 0:
        print("::error::Báo cáo coverage không chứa dòng nào có thể phủ.")
        sys.exit(1)

    percent = covered * 100.0 / total
    emit("coverage_percent", f"{percent:.2f}")

    passed = percent + 1e-9 >= args.min
    icon = "✅" if passed else "❌"
    summary(
        f"### {icon} Coverage · {args.label}\n\n"
        f"| Chỉ số | Giá trị |\n|---|---|\n"
        f"| Dòng đã phủ | {covered} |\n"
        f"| Tổng số dòng | {total} |\n"
        f"| Coverage | **{percent:.2f}%** |\n"
        f"| Ngưỡng tối thiểu | {args.min:.2f}% |\n"
        f"| Số file report | {len(reports)} |\n"
    )

    print(f"{args.label}: {percent:.2f}% (ngưỡng {args.min:.2f}%)")

    if not passed:
        print(
            f"::error::Coverage {percent:.2f}% thấp hơn ngưỡng {args.min:.2f}% "
            f"khai báo trong .github/quality-gates.yml."
        )
        sys.exit(1)


if __name__ == "__main__":
    main()
