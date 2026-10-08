#!/usr/bin/env bash
#
# Dựng database cho MÁY LOCAL: migration → vai trò → dữ liệu seed.
#
# Chạy trong service `migrate` của docker-compose, một lần rồi thoát — cùng hình dạng với
# ECS one-off task chạy trước bước deploy trên môi trường thật (ADR-010). Dùng đúng một cơ
# chế ở cả hai nơi là có chủ đích: migration chạy kiểu khác ở local so với trên cloud là
# cách tạo ra loại lỗi chỉ xuất hiện sau khi deploy.
#
# Ba bước tách bạch vì chúng có vòng đời khác nhau:
#
#   1. db/migrations/*.sql  — lược đồ, xuất từ EF Core, chạy ở MỌI môi trường
#   2. db/roles.sql         — ai được chạm vào lược đồ, chạy ở mọi môi trường (mật khẩu
#                             local nằm trong file; trên AWS lấy từ Secrets Manager)
#   3. db/seed/*.sql        — dữ liệu mẫu, CHỈ local và dev. Không bao giờ chạy ở production.
#
# Dùng:
#   DATABASE_URL=postgres://postgres:...@postgres:5432/xnk bash db/bootstrap-local.sh
#
# Chạy lại nhiều lần an toàn: cả ba bước đều idempotent.

set -euo pipefail

THU_MUC="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ -z "${DATABASE_URL:-}" ]; then
  echo "✗ Thiếu DATABASE_URL. Script này chạy bên trong container, không có docker CLI để" >&2
  echo "  gọi ngược ra ngoài như db/apply-migrations.sh làm khi chạy trên máy." >&2
  exit 1
fi

echo "═══ 1/3 · Lược đồ ═══"
bash "$THU_MUC/apply-migrations.sh"

echo
echo "═══ 2/3 · Vai trò và quyền ═══"
# ON_ERROR_STOP: một GRANT hỏng giữa chừng mà script vẫn báo thành công thì service sẽ
# chết ở request đầu tiên với lỗi permission denied, cách xa nguyên nhân hàng chục phút.
psql "$DATABASE_URL" --quiet --set ON_ERROR_STOP=1 --file "$THU_MUC/roles.sql"
echo "✓ Xong."

echo
echo "═══ 3/3 · Dữ liệu seed ═══"

# ⚠️ CỔNG SEED — thứ duy nhất ngăn dữ liệu mẫu vào một database thật.
#
# Bản trước chỉ đếm SỐ FILE rồi chạy hết, không kiểm môi trường lần nào. Cái duy nhất giữ
# cho nó không nạp SOP giả vào database thật là chữ "local" trong tên file — và tên file
# thì không thi hành được gì. Tệ hơn: `db/Dockerfile` đặt ENTRYPOINT trỏ thẳng vào script
# này, và chính tài liệu nói ảnh đó là mẫu cho task migration trên ECS.
#
# Nay cần MỘT trong hai điều kiện, cả hai đều tường minh:
#   · XNK_CHO_PHEP_SEED=1  — quyết định có tên, ai đặt cũng biết mình đang cho phép gì
#   · tên database nằm trong danh sách an toàn
XNK_DB_SEED_AN_TOAN="${XNK_DB_SEED_AN_TOAN:-xnk xnk_test xnk_dev}"
ten_db="$(basename "${DATABASE_URL%%\?*}")"

cho_phep=0
if [ "${XNK_CHO_PHEP_SEED:-0}" = "1" ]; then
  cho_phep=1
else
  for an_toan in $XNK_DB_SEED_AN_TOAN; do
    if [ "$ten_db" = "$an_toan" ]; then
      cho_phep=1
      break
    fi
  done
fi

if [ "$cho_phep" != "1" ]; then
  echo "Bỏ qua seed: database '$ten_db' không nằm trong danh sách an toàn ($XNK_DB_SEED_AN_TOAN)." >&2
  echo "Cố ý seed vào database này thì đặt XNK_CHO_PHEP_SEED=1." >&2
  exit 0
fi
so_file=$(find "$THU_MUC/seed" -maxdepth 1 -name '*.sql' 2>/dev/null | wc -l | tr -d ' ')
if [ "$so_file" = "0" ]; then
  echo "Không có file seed nào — bỏ qua."
else
  # Thứ tự theo tên file: 0001 trước 0002. Seed sau có thể phụ thuộc seed trước.
  for f in "$THU_MUC"/seed/*.sql; do
    echo "  · $(basename "$f")"
    psql "$DATABASE_URL" --quiet --set ON_ERROR_STOP=1 --file "$f"
  done
  echo "✓ Xong $so_file file seed."
fi

echo
echo "✓ Database local đã sẵn sàng."
