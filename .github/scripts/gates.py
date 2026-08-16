#!/usr/bin/env python3
"""Đọc một giá trị cấu hình từ file YAML hoặc JSON theo đường dẫn dấu chấm.

Vì sao cần: ngưỡng chất lượng, phiên bản runtime và danh mục service đều nằm trong
file cấu hình được review qua PR (.github/quality-gates.yml, .github/services.json,
eval/gates.yml). Nếu mỗi workflow tự parse thì logic bị nhân bản 6 lần và sẽ lệch nhau.

Dùng:
    python3 .github/scripts/gates.py get runtimes.dotnet
    python3 .github/scripts/gates.py get blazor.bundle_budget_brotli_kb
    python3 .github/scripts/gates.py get solution --file .github/services.json
    python3 .github/scripts/gates.py json services --file .github/services.json

Lệnh `get` in ra một giá trị vô hướng (bool thành true/false chữ thường để shell so sánh
được). Lệnh `json` in ra JSON gọn một dòng, dùng làm matrix cho GitHub Actions.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

DEFAULT_FILE = ".github/quality-gates.yml"


def load(path: Path) -> Any:
    if not path.is_file():
        sys.exit(f"gates.py: không tìm thấy file cấu hình '{path}'")

    text = path.read_text(encoding="utf-8")

    if path.suffix in {".yml", ".yaml"}:
        try:
            import yaml
        except ModuleNotFoundError:
            sys.exit(
                "gates.py: thiếu PyYAML. Thêm bước `python3 -m pip install --quiet pyyaml` "
                "vào workflow trước khi gọi script này."
            )
        return yaml.safe_load(text)

    if path.suffix == ".json":
        return json.loads(text)

    sys.exit(f"gates.py: không hỗ trợ đuôi file '{path.suffix}'")


def resolve(data: Any, dotted: str, source: Path) -> Any:
    current = data
    walked: list[str] = []
    for part in dotted.split("."):
        walked.append(part)
        if isinstance(current, list):
            try:
                current = current[int(part)]
                continue
            except (ValueError, IndexError):
                sys.exit(f"gates.py: '{'.'.join(walked)}' không hợp lệ trong {source}")
        if not isinstance(current, dict) or part not in current:
            sys.exit(f"gates.py: không có khoá '{'.'.join(walked)}' trong {source}")
        current = current[part]
    return current


def render(value: Any) -> str:
    if isinstance(value, bool):
        return "true" if value else "false"
    if value is None:
        return ""
    if isinstance(value, (dict, list)):
        return json.dumps(value, ensure_ascii=False, separators=(",", ":"))
    return str(value)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=["get", "json"])
    parser.add_argument("path", help="Đường dẫn dấu chấm, ví dụ runtimes.dotnet")
    parser.add_argument("--file", default=DEFAULT_FILE)
    args = parser.parse_args()

    source = Path(args.file)
    value = resolve(load(source), args.path, source)

    if args.command == "json":
        print(json.dumps(value, ensure_ascii=False, separators=(",", ":")))
    else:
        print(render(value))


if __name__ == "__main__":
    main()
