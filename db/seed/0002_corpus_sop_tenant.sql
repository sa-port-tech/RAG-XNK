-- ⚠️ FILE SINH TỰ ĐỘNG — sinh lại bằng: python tools/local/sinh_seed.py
--
-- SOP nội bộ của từng tenant, DÙNG CHO MÁY LOCAL. Đây KHÔNG phải văn bản pháp luật;
-- trích yếu mang tiền tố [MẪU LOCAL] để không ai nhầm khi nhìn thấy trong giao diện.
--
-- Vì sao cần: bộ lọc cách ly tenant chỉ chứng minh được điều gì khi có dữ liệu RIÊNG của
-- tenant. Toàn bộ 16 văn bản ở seed 0001 đều dùng chung (TenantId IS NULL), nên tự chúng
-- không phân biệt được hai tenant.
--
-- Khoá tenant (suy ra bằng UUIDv5 từ slug, xem tools/local/sinh_seed.py):
--   noi-bo           82434b47-80ea-518c-95c3-7b2cc0d19263  (noi_bo)
--   b2b-khach-hang   571a284e-9e5b-5e68-a0e7-36d7dcb77751  (b2b_khach_hang)
--
-- Không có khoá ngoại sang `identity.tenants`: hai schema thuộc hai service khác nhau và
-- docs/00 §4.4 cấm ràng buộc chéo schema. Ràng buộc đó nằm ở tầng nghiệp vụ.

INSERT INTO corpus.documents
    ("Id", "TenantId", "DocumentNumber", "Title", "EffectiveFrom", "EffectiveTo")
VALUES
    ('1d6d4d0a-a75e-59bb-bacb-c6a372bcf880', '82434b47-80ea-518c-95c3-7b2cc0d19263', 'SOP-NB-01', '[MẪU LOCAL] Quy trình nội bộ: kiểm tra bộ chứng từ trước khi mở tờ khai', NULL, NULL),
    ('37d11713-e4d7-5587-89e3-d8f3156f2eda', '82434b47-80ea-518c-95c3-7b2cc0d19263', 'SOP-NB-02', '[MẪU LOCAL] Quy trình nội bộ: xử lý tờ khai luồng đỏ', NULL, NULL),
    ('3f1f298d-2798-5e5e-87e5-34d7ff4a3659', '571a284e-9e5b-5e68-a0e7-36d7dcb77751', 'SOP-KH-01', '[MẪU LOCAL] Hướng dẫn khách hàng: chuẩn bị hồ sơ nhập khẩu hàng bách hoá', NULL, NULL),
    ('22166979-41e3-5147-b3d5-09bb84210a37', '571a284e-9e5b-5e68-a0e7-36d7dcb77751', 'SOP-KH-02', '[MẪU LOCAL] Hướng dẫn khách hàng: khai báo trị giá hải quan', NULL, NULL)
ON CONFLICT ("Id") DO NOTHING;
