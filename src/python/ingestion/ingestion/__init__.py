"""Service ingestion — hiện mới là khung, chưa nạp gì cả.

⚠️ Docstring này từng ghi "crawl, parse, phân rã cấu trúc pháp lý". Toàn bộ cài đặt hiện
tại là **một readiness probe gọi sang API của corpus** — không crawler, không parser, không
phân rã Điều/Khoản.

Ba việc đó là phạm vi epic E2. Trách nhiệm đầy đủ của service ghi ở ``docs/00`` §4.2;
docstring này nói về thứ đang chạy, không nói về thứ sẽ chạy.
"""
