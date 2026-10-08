#!/usr/bin/env python3
"""Đếm số test MANG ĐÚNG MỘT TRAIT đã chạy, đọc từ các file .trx dưới một thư mục.

Dùng bởi bước "Bảo đảm cổng BR-11 không rỗng" của `ci-dotnet`.

Vì sao cần nó
-------------
Cổng cách ly tenant phải đỏ khi **không có test BR-11 nào ở đâu cả** — nếu không, lớp phòng
thủ quan trọng nhất của hệ thống sẽ tự tắt trong im lặng đúng lúc nó cần nhất
(`docs/16` §3, `docs/00` §12).

Cách cũ là `RunConfiguration.TreatNoTestsAsError=true` trên cả solution. Cờ đó bắt **mọi**
assembly test phải có test mang trait, nên project test thứ hai được thêm vào — dù hoàn toàn
hợp lệ — cũng làm đỏ cổng. Đó là một ý đồ khác hẳn, và là ý đồ sai.

Vì sao ĐẾM THEO LỚP chứ không đọc `ResultSummary/Counters`
-----------------------------------------------------------
Bản trước cộng thuộc tính `executed` của `ResultSummary` — tức **tổng số test trong file
trx**, bất kể chúng là test gì. Con số đó chỉ đúng khi lời gọi `dotnet test` có
`--filter "Category=TenantIsolation"`, và **không có gì trong repo bảo đảm điều đó**: xoá
dòng `--filter` ở `ci-dotnet.yml` là cổng vui vẻ đếm hơn tám mươi test chẳng liên quan rồi
báo xanh. Một cổng bảo mật mà chứng cứ của nó phụ thuộc vào một dòng ở file khác thì nó
không phải cổng.

Cách đúng về mặt ý niệm là đếm theo **trait** `Category=TenantIsolation`. Không làm được:
đo ngày 10/09/2026 trên trx thật, phần tử `<UnitTest>` do bộ adapter xUnit sinh ra chỉ có
`<Execution>` và `<TestMethod>` — **không có `<TestCategory>`**. Trait sống trong bộ lọc
lúc chạy, không đi vào báo cáo.

Nên script neo vào thứ trx CÓ ghi: tên lớp trong `TestMethod/@className`. Lớp sở hữu BR-11
là `TenantIsolationTests`. Bỏ `--filter` đi thì trx đầy test của lớp khác, và script vẫn
đếm đúng những test thuộc lớp này — đó là tính chất cần có.

Cái giá, nêu thẳng: đổi tên lớp mà quên đổi tham số ở `ci-dotnet.yml` sẽ làm cổng đỏ. Đó
là hướng hỏng đúng — đỏ và nói rõ lý do, chứ không phải xanh vì đếm nhầm thứ khác.

    python3 .github/scripts/dem_test_trx.py artifacts/tenant-isolation TenantIsolationTests
"""

from __future__ import annotations

import glob
import os
import sys
import xml.etree.ElementTree as ET

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}

# Lớp sở hữu các test BR-11. Khớp theo chuỗi con của `TestMethod/@className`, nên một lớp
# BR-11 thứ hai đặt tên theo cùng quy ước cũng được đếm.
LOP_MAC_DINH = "TenantIsolationTests"


def _id_test_thuoc_lop(goc: ET.Element, lop: str) -> set[str]:
    """Id của những test có tên lớp chứa ``lop``."""
    ids: set[str] = set()
    for dinh_nghia in goc.findall("t:TestDefinitions/t:UnitTest", NS):
        ma = dinh_nghia.get("id")
        phuong_thuc = dinh_nghia.find("t:TestMethod", NS)
        if not ma or phuong_thuc is None:
            continue
        if lop in (phuong_thuc.get("className") or ""):
            ids.add(ma)
    return ids


def dem(thu_muc: str, lop: str = LOP_MAC_DINH) -> int:
    """Số test thuộc lớp ``lop`` đã thực sự chạy, cộng qua mọi file trx tìm thấy."""
    tong = 0
    thay_file = False

    for duong_dan in glob.glob(os.path.join(thu_muc, "**", "*.trx"), recursive=True):
        thay_file = True
        try:
            goc = ET.parse(duong_dan).getroot()
        except ET.ParseError as loi:
            # trx hỏng thì coi như không đếm được, và nói ra — im lặng bỏ qua sẽ biến một
            # file hỏng thành "không có test nào", tức là báo đỏ vì lý do sai.
            print(f"::warning::Không đọc được {duong_dan}: {loi}", file=sys.stderr)
            continue

        ids = _id_test_thuoc_lop(goc, lop)
        if not ids:
            continue

        for ket_qua in goc.findall("t:Results/t:UnitTestResult", NS):
            if ket_qua.get("testId") in ids:
                tong += 1

    if not thay_file:
        # Khác hẳn "có file nhưng không test nào mang trait": không có trx nào nghĩa là
        # bước chạy test phía trên đã hỏng, và báo "0 test BR-11" sẽ chỉ sai chỗ.
        print(
            f"::warning::Không tìm thấy file .trx nào dưới {thu_muc} — "
            "bước chạy test có thể đã hỏng trước đó.",
            file=sys.stderr,
        )

    return tong


if __name__ == "__main__":
    if len(sys.argv) not in (2, 3):
        sys.exit("Dùng: dem_test_trx.py <thư mục chứa .trx> [tên lớp]")

    print(dem(sys.argv[1], sys.argv[2] if len(sys.argv) == 3 else LOP_MAC_DINH))
