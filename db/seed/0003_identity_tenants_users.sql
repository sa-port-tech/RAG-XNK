-- ⚠️ FILE SINH TỰ ĐỘNG — sinh lại bằng: python tools/local/sinh_seed.py
--
-- Tenant và tài khoản mẫu cho MÁY LOCAL.
--
-- Mật khẩu của cả bốn tài khoản: matkhau-local-2026
--
--   an.nguyen@noibo.vn       admin   noi-bo
--   binh.tran@noibo.vn       user    noi-bo
--   chi.le@cangxanh.vn       admin   b2b-khach-hang
--   dung.pham@cangxanh.vn    viewer  b2b-khach-hang
--
-- Băm bằng PBKDF2-HMAC-SHA256, 600,000 vòng, đúng định dạng mà
-- src/dotnet/Xnk.IdentityTenant/Security/PasswordHasher.cs đọc được. Định dạng chuỗi là
-- hợp đồng liên ngôn ngữ giữa file này và mã C# — đổi một bên mà quên bên kia thì không
-- ai đăng nhập được, và lỗi trông giống hệt "sai mật khẩu".
--
-- Muối ở đây suy ra từ email nên file sinh lại luôn giống nhau; mã C# khi tạo người dùng
-- thật thì dùng muối ngẫu nhiên. Xem chú thích hàm bam_mat_khau().
--
-- Chạy lại nhiều lần an toàn: khoá chính ổn định (UUIDv5) + ON CONFLICT DO NOTHING.

INSERT INTO identity.tenants ("Id", "Slug", "Name", "Type")
VALUES
    ('82434b47-80ea-518c-95c3-7b2cc0d19263', 'noi-bo', 'Phòng XNK — nội bộ', 'noi_bo'),
    ('571a284e-9e5b-5e68-a0e7-36d7dcb77751', 'b2b-khach-hang', 'Công ty Giao nhận Cảng Xanh', 'b2b_khach_hang')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO identity.users ("Id", "TenantId", "Email", "PasswordHash", "Role", "IsActive")
VALUES
    ('dea6052b-6868-5a39-9a47-cd5c4021258f', '82434b47-80ea-518c-95c3-7b2cc0d19263', 'an.nguyen@noibo.vn', 'pbkdf2_sha256$600000$fWGzkSRv2rDbLc44flzaAA==$Wr+/XGLf8005unV3fVN+lzUuBYvpNxegovM6m1Buo7I=', 'admin', true),
    ('89ad7aca-c21d-575d-b463-faa172872a5e', '82434b47-80ea-518c-95c3-7b2cc0d19263', 'binh.tran@noibo.vn', 'pbkdf2_sha256$600000$DLY9iJMRCd7wBRLt3dHshQ==$y0XJr6QHqXuYa0alsmB5RLp3POfBahkyfAc6xBWJMHk=', 'user', true),
    ('98db651a-4157-5442-b710-32ed236ca64c', '571a284e-9e5b-5e68-a0e7-36d7dcb77751', 'chi.le@cangxanh.vn', 'pbkdf2_sha256$600000$XOYPuy7026TDS9MfNJSvUA==$OloTy0UVODYnmEo0omQvLXcBvkYg8UMceSRd8wwyCKA=', 'admin', true),
    ('193e840f-1338-555c-9853-6b26113cea2c', '571a284e-9e5b-5e68-a0e7-36d7dcb77751', 'dung.pham@cangxanh.vn', 'pbkdf2_sha256$600000$djnYkVyLew6uWSo3kUkrSA==$nl9B8vmmJ61d+qOts4aUi1h1glIFcOAaMwAXxMwbiLY=', 'viewer', true)
ON CONFLICT ("Id") DO NOTHING;
