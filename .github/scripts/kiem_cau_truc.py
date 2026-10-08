#!/usr/bin/env python3
"""Kiểm ba thứ mà không cổng nào khác trong repo bắt được.

    python3 .github/scripts/kiem_cau_truc.py

1. **Cổng local không trùng nhau và khớp `launchSettings.json`.**
   Bốn service .NET tự chọn cổng localhost bằng tay: 5062, 5063, 5064, 5065. Không gì
   phối hợp chúng và không gì báo khi hai service chọn trùng — người gặp sẽ thấy
   "address already in use" và đi tìm ở chỗ khác. Nay `local_port` nằm trong
   `.github/services.json`, cùng chỗ với `container_port`, và script này bắt lệch.

2. **Mọi `.csproj` đều có trong `Xnk.sln`.**
   Solution file bảo trì tay. Project không nằm trong đó thì `dotnet build` bỏ qua nó,
   `dotnet test` không chạy test của nó, và CI xanh — một project vô hình.

3. **Mọi service trong manifest đều có thư mục thật.**
   `services.json` là nguồn sự thật cho path filter của CI, ma trận build của CD và định
   tuyến nginx (ADR-009). Một dòng trỏ vào thư mục không tồn tại làm cả ba sai cùng lúc.
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

GOC = Path(__file__).resolve().parents[2]
MANIFEST = GOC / ".github" / "services.json"
SLN = GOC / "src" / "dotnet" / "Xnk.sln"


def _loi(thong_bao: str) -> None:
    print(f"::error::{thong_bao}", file=sys.stderr)


def kiem_cong(manifest: dict) -> list[str]:
    """Cổng local: không trùng nhau, và khớp launchSettings nếu file đó tồn tại."""
    loi: list[str] = []
    da_thay: dict[int, str] = {}

    for sv in manifest["services"]:
        cong = sv.get("local_port")
        if cong is None:
            loi.append(f"Service {sv['name']} thiếu trường local_port trong services.json.")
            continue

        if cong in da_thay:
            loi.append(
                f"Cổng local {cong} bị hai service dùng chung: {da_thay[cong]} và {sv['name']}."
            )
        da_thay[cong] = sv["name"]

        launch = GOC / sv["path"] / "Properties" / "launchSettings.json"
        if not launch.is_file():
            continue

        cong_thuc = {int(m) for m in re.findall(r"http://localhost:(\d+)", launch.read_text("utf-8"))}
        if cong_thuc and cong not in cong_thuc:
            loi.append(
                f"{sv['name']}: services.json ghi local_port={cong} nhưng "
                f"{launch.relative_to(GOC)} dùng {sorted(cong_thuc)}."
            )

    return loi


def kiem_solution() -> list[str]:
    """Mọi .csproj dưới src/dotnet đều được khai trong Xnk.sln."""
    if not SLN.is_file():
        return [f"Không thấy {SLN.relative_to(GOC)}."]

    noi_dung = SLN.read_text("utf-8")
    thieu = [
        str(p.relative_to(GOC))
        for p in sorted((GOC / "src" / "dotnet").glob("*/*.csproj"))
        if p.name not in noi_dung
    ]

    return [f"Project không có trong Xnk.sln (build sẽ bỏ qua): {p}" for p in thieu]


def kiem_duong_dan(manifest: dict) -> list[str]:
    """Mọi service trong manifest trỏ vào một thư mục có thật."""
    return [
        f"Service {sv['name']}: đường dẫn {sv['path']} không tồn tại."
        for sv in manifest["services"]
        if not (GOC / sv["path"]).is_dir()
    ]


def main() -> int:
    manifest = json.loads(MANIFEST.read_text("utf-8"))

    tat_ca = kiem_cong(manifest) + kiem_solution() + kiem_duong_dan(manifest)
    for muc in tat_ca:
        _loi(muc)

    if tat_ca:
        return 1

    print(f"✓ {len(manifest['services'])} service: cổng local không trùng, khớp launchSettings.")
    print("✓ Mọi .csproj đều có trong Xnk.sln.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
