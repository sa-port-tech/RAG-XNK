-- Vai trò database cho từng service, và quyền của chúng trên từng schema.
--
-- ── Vì sao file này tồn tại ────────────────────────────────────────────────────────────
-- docs/00 §4.4: "Mỗi service có DB user riêng, chỉ được cấp quyền trên schema của mình.
-- Service khác muốn đọc → gọi API, không JOIN chéo schema."
--
-- Nếu ở máy dev mọi service cùng nối bằng `postgres`, ranh giới đó chỉ là một câu trong
-- tài liệu. Lần đầu nó được thực thi thật sẽ là lúc lên AWS — tức chỗ đắt nhất và muộn
-- nhất để phát hiện ra rằng đã có ai đó JOIN chéo schema từ ba tháng trước.
--
-- ── Vì sao KHÔNG nằm trong db/migrations/ ──────────────────────────────────────────────
-- Migration mô tả **lược đồ**; file này mô tả **ai được chạm vào lược đồ đó**. Hai thứ có
-- vòng đời khác nhau: lược đồ theo mã nguồn, còn danh sách vai trò theo môi trường. Trộn
-- vào nhau thì mỗi lần đổi quyền lại phải sinh một migration EF không đổi bảng nào.
--
-- Nó cũng tránh kéo mọi thay đổi quyền qua CODEOWNERS của /db/migrations/ (yêu cầu ≥2
-- approval, thêm data-lead và backend-lead) — mức duyệt đó dành cho thay đổi lược đồ.
--
-- ── Mật khẩu ───────────────────────────────────────────────────────────────────────────
-- Các mật khẩu dưới đây KHÔNG phải bí mật: chúng chỉ dùng cho PostgreSQL trong
-- docker-compose ở máy dev, và nằm trong git có chủ đích để `docker compose up` chạy được
-- ngay sau khi clone. Trên dev/staging/production, mật khẩu đến từ AWS Secrets Manager và
-- file này chỉ đóng vai trò danh sách vai trò cần tạo.
--
-- ── Chạy lại nhiều lần ─────────────────────────────────────────────────────────────────
-- An toàn. Vai trò đã có thì bỏ qua; schema chưa có thì báo NOTICE rồi đi tiếp — vì các
-- schema `identity`, `conversation`, `lookup`, `vector` chỉ xuất hiện khi story tương ứng
-- được làm. Chạy lại `db/bootstrap-local.sh` sau mỗi migration mới là đủ để quyền theo kịp.

-- ══════════════════════════════════════════════════════════════════════════════════════
-- 1. Tạo vai trò
-- ══════════════════════════════════════════════════════════════════════════════════════
--
-- Chỉ bốn service **sở hữu hoặc đọc dữ liệu** mới có vai trò. Ba service còn lại không có,
-- và đó là chủ đích, không phải thiếu sót (docs/00 §4.2):
--
--   ingestion       → ghi qua API của corpus-service, không chạm database
--   generation      → không sở hữu dữ liệu nào
--   workflow-worker → không sở hữu dữ liệu nào
--
-- Cấp cho chúng một vai trò database "cho tiện" là mở lại đúng cánh cửa mà §4.4 đóng.

DO $tao_vai_tro$
DECLARE
    r record;
BEGIN
    FOR r IN
        SELECT * FROM (VALUES
            ('xnk_identity',  'xnk-local-identity'),
            ('xnk_corpus',    'xnk-local-corpus'),
            ('xnk_chat',      'xnk-local-chat'),
            ('xnk_retrieval', 'xnk-local-retrieval')
        ) AS t(vai_tro, mat_khau)
    LOOP
        IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = r.vai_tro) THEN
            -- Đặt lại mật khẩu để một database cũ vẫn khớp với file .env hiện tại.
            EXECUTE format('ALTER ROLE %I WITH LOGIN PASSWORD %L', r.vai_tro, r.mat_khau);
            RAISE NOTICE 'Vai trò % đã có, cập nhật mật khẩu.', r.vai_tro;
        ELSE
            EXECUTE format('CREATE ROLE %I WITH LOGIN PASSWORD %L', r.vai_tro, r.mat_khau);
            RAISE NOTICE 'Đã tạo vai trò %.', r.vai_tro;
        END IF;
    END LOOP;
END
$tao_vai_tro$;

-- ══════════════════════════════════════════════════════════════════════════════════════
-- 2. Cấp quyền theo schema
-- ══════════════════════════════════════════════════════════════════════════════════════
--
-- `crud` = SELECT/INSERT/UPDATE/DELETE trên bảng, không có quyền DDL. Service **không**
-- được tạo hay đổi bảng: lược đồ thuộc EF Core migrations và chạy bằng vai trò chủ sở hữu
-- trong một task riêng (ADR-010). Service tự đổi lược đồ lúc chạy là thứ phải bất khả thi,
-- không phải thứ trông cậy vào kỷ luật.
--
-- Dòng `xnk_retrieval` + `corpus` ở mức `read` là **ngoại lệ có chủ đích** của §4.4, đã
-- được ADR-012 chốt: lọc hiệu lực phải nằm trong cùng một câu SQL với vector search, tách
-- ra sẽ phá lớp phòng thủ quan trọng nhất của hệ thống (docs/00 §10.3). Đây là chỗ duy
-- nhất một service được đọc schema của service khác — thêm dòng thứ hai vào bảng này là
-- một quyết định kiến trúc, cần ADR riêng.

DO $cap_quyen$
DECLARE
    r record;
    chu_so_huu text := current_user;
BEGIN
    FOR r IN
        SELECT * FROM (VALUES
            ('xnk_identity',  'identity',     'crud'),
            ('xnk_corpus',    'corpus',       'crud'),
            ('xnk_corpus',    'lookup',       'crud'),
            ('xnk_chat',      'conversation', 'crud'),
            ('xnk_retrieval', 'corpus',       'read'),   -- ngoại lệ ADR-012
            ('xnk_retrieval', 'vector',       'crud')
        ) AS t(vai_tro, luoc_do, quyen)
    LOOP
        IF NOT EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = r.luoc_do) THEN
            RAISE NOTICE 'Bỏ qua % trên %: schema chưa tồn tại.', r.vai_tro, r.luoc_do;
            CONTINUE;
        END IF;

        EXECUTE format('GRANT USAGE ON SCHEMA %I TO %I', r.luoc_do, r.vai_tro);

        IF r.quyen = 'crud' THEN
            EXECUTE format(
                'GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %I TO %I',
                r.luoc_do, r.vai_tro);
            EXECUTE format(
                'GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA %I TO %I',
                r.luoc_do, r.vai_tro);
            -- Bảng sinh ra bởi migration SAU lần chạy này cũng phải được cấp quyền, nếu
            -- không thì mỗi migration mới lại kèm một sự cố "service đọc không được bảng
            -- vừa tạo" mà không ai nối được với nguyên nhân.
            EXECUTE format(
                'ALTER DEFAULT PRIVILEGES FOR ROLE %I IN SCHEMA %I '
                'GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO %I',
                chu_so_huu, r.luoc_do, r.vai_tro);
            EXECUTE format(
                'ALTER DEFAULT PRIVILEGES FOR ROLE %I IN SCHEMA %I '
                'GRANT USAGE, SELECT ON SEQUENCES TO %I',
                chu_so_huu, r.luoc_do, r.vai_tro);
        ELSE
            EXECUTE format(
                'GRANT SELECT ON ALL TABLES IN SCHEMA %I TO %I', r.luoc_do, r.vai_tro);
            EXECUTE format(
                'ALTER DEFAULT PRIVILEGES FOR ROLE %I IN SCHEMA %I '
                'GRANT SELECT ON TABLES TO %I',
                chu_so_huu, r.luoc_do, r.vai_tro);
        END IF;

        RAISE NOTICE 'Đã cấp % cho % trên schema %.', r.quyen, r.vai_tro, r.luoc_do;
    END LOOP;
END
$cap_quyen$;
