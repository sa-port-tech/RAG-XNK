#!/usr/bin/env python3
"""Đếm tổng số test đã chạy trong các file .trx dưới một thư mục.

Dùng bởi bước "Bảo đảm cổng BR-11 không rỗng" của `ci-dotnet`.

Vì sao cần nó
-------------
Cổng cách ly tenant phải đỏ khi **không có test BR-11 nào ở đâu cả** — nếu không, lớp phòng
thủ quan trọng nhất của hệ thống sẽ tự tắt trong im lặng đúng lúc nó cần nhất
(`docs/16` §3, `docs/00` §12).

Cách cũ là `RunConfiguration.TreatNoTestsAsError=true` trên cả solution. Cờ đó bắt **mọi**
assembly test phải có test mang trait, nên project test thứ hai được thêm vào — dù hoàn toàn
hợp lệ — cũng làm đỏ cổng. Đó là một ý đồ khác hẳn, và là ý đồ sai.

Đếm từ trx cho ra đúng điều cần: tổng số test khớp bộ lọc trên toàn solution. Test hỏng thì
`dotnet test` đã đỏ trước khi tới đây rồi.

    python3 .github/scripts/dem_test_trx.py artifacts/tenant-isolation
"""

from __future__ import annotations

import glob
import os
import sys
import xml.etree.ElementTree as ET

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


def dem(thu_muc: str) -> int:
    """Tổng số test đã thực thi, cộng qua mọi file trx tìm thấy."""
    tong = 0
    for duong_dan in glob.glob(os.path.join(thu_muc, "**", "*.trx"), recursive=True):
        try:
            goc = ET.parse(duong_dan).getroot()
        except ET.ParseError as loi:
            # trx hỏng thì coi như không đếm được, và nói ra — im lặng bỏ qua sẽ biến một
            # file hỏng thành "không có test nào", tức là báo đỏ vì lý do sai.
            print(f"::warning::Không đọc được {duong_dan}: {loi}", file=sys.stderr)
            continue

        bo_dem = goc.find("t:ResultSummary/t:Counters", NS)
        if bo_dem is not None:
            tong += int(bo_dem.get("executed", 0))

    return tong


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit("Dùng: dem_test_trx.py <thư mục chứa .trx>")

    print(dem(sys.argv[1]))
