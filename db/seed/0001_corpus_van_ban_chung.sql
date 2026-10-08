-- ⚠️ FILE SINH TỰ ĐỘNG — sinh lại bằng: python tools/local/sinh_seed.py
--
-- Nguồn: corpus/registry/van-ban.yaml — 17 bản ghi, đưa vào 7,
-- bỏ qua 10 slot chưa xác định được số hiệu (chỉ có mo_ta_slot).
-- Sửa registry rồi sinh lại; đừng sửa file này.
--
-- Đây là văn bản quy phạm pháp luật DÙNG CHUNG cho mọi tenant, nên `TenantId` là NULL —
-- ngữ nghĩa của bộ lọc trong CorpusDbContext.OnModelCreating (ADR-012).
--
-- `EffectiveFrom` / `EffectiveTo` để NULL vì chuyên gia XNK chưa xác minh hiệu lực trên
-- vbpl.vn. Đó là dữ kiện chưa có, không phải ô cần điền cho đẹp.
--
-- Chạy lại nhiều lần an toàn nhờ khoá chính ổn định (UUIDv5) + ON CONFLICT DO NOTHING.

INSERT INTO corpus.documents
    ("Id", "TenantId", "DocumentNumber", "Title", "EffectiveFrom", "EffectiveTo")
VALUES
    ('908cc8b8-cacd-5629-a411-574a1e568e18', NULL, '54/2014/QH13', 'Luật Hải quan', NULL, NULL),
    ('4b5ee9f0-8f23-5988-a9f8-34221e3fb2b6', NULL, '54/VBHN-VPQH', 'hợp nhất Luật Hải quan', NULL, NULL),
    ('aef0047e-1109-5232-ba65-0939344931be', NULL, '08/2015/NĐ-CP', 'quy định chi tiết và biện pháp thi hành Luật Hải quan về thủ tục hải quan, kiểm tra, giám sát, kiểm soát hải quan', DATE '2015-03-15', NULL),
    ('bc4fd916-830b-5135-a885-e98e89ee80bb', NULL, '59/2018/NĐ-CP', 'sửa đổi, bổ sung một số điều của Nghị định số 08/2015/NĐ-CP ngày 21 tháng 01 năm 2015 của Chính phủ quy định chi tiết và biện pháp thi hành Luật hải quan về thủ tục hải quan, kiểm tra, giám sát, kiểm soát hải quan', DATE '2018-06-05', NULL),
    ('17edd12b-81b1-5bad-a562-d1b9d07032cf', NULL, '38/2015/TT-BTC', 'quy định về thủ tục hải quan; kiểm tra, giám sát hải quan; thuế xuất khẩu, thuế nhập khẩu và quản lý thuế đối với hàng hóa xuất khẩu, nhập khẩu', DATE '2015-04-01', NULL),
    ('8ca4bc2d-6411-5ad9-9ff1-f87fe593a079', NULL, '39/2018/TT-BTC', 'sửa đổi, bổ sung một số điều tại Thông tư số 38/2015/TT-BTC ngày 25 tháng 3 năm 2015 quy định về thủ tục hải quan; kiểm tra, giám sát hải quan; thuế xuất khẩu, thuế nhập khẩu', DATE '2018-06-05', NULL),
    ('b9fbff85-8d38-5834-881c-51f363f33c07', NULL, '25/VBHN-BTC', 'Thông tư quy định về thủ tục hải quan; kiểm tra, giám sát hải quan; thuế xuất khẩu, thuế nhập khẩu và quản lý thuế đối với hàng hóa xuất khẩu, nhập khẩu', NULL, NULL)
ON CONFLICT ("Id") DO NOTHING;
