#!/usr/bin/env bash
#
# Chạy bộ test .NET trong container, dùng PostgreSQL của docker-compose.
#
#   bash tools/local/chay-test.sh
#   bash tools/local/chay-test.sh "Category=TenantIsolation"
#
# Xem tools/local/test.Dockerfile để biết vì sao có đường này bên cạnh `dotnet test`.
#
# Điều kiện: `docker compose up -d postgres` đang chạy (hoặc profile app).

set -euo pipefail

GOC="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$GOC"

BO_LOC="${1:-}"

# Bộ lọc gõ sai thì `dotnet test` chạy 0 test và VẪN XANH — một lần chạy "thành công" mà
# không kiểm gì cả, và người gõ nhầm không có cách nào biết. Đây đúng là lỗ hổng mà
# `.github/scripts/dem_test_trx.py` tồn tại để bịt ở phía CI; ở phía máy dev thì chưa có gì.
#
# Không tự đoán ý người dùng: chỉ chặn khi bộ lọc trông như `Category=...` mà giá trị
# không nằm trong danh sách trait đang có thật. Mọi cú pháp lọc khác đi thẳng như cũ.
TRAIT_CO_THAT="TenantIsolation"

case "$BO_LOC" in
  Category=*)
    gia_tri="${BO_LOC#Category=}"
    case " $TRAIT_CO_THAT " in
      *" $gia_tri "*) ;;
      *)
        echo "Trait '$gia_tri' không tồn tại trong repo. Trait đang có: $TRAIT_CO_THAT" >&2
        echo "Gõ sai trait thì dotnet test chạy 0 test và vẫn báo xanh — nên dừng ở đây." >&2
        exit 1
        ;;
    esac
    ;;
esac

# Database RIÊNG cho test. Không dùng chung `xnk` với môi trường dev: bộ test ghi hàng
# trăm bản ghi rác, và trộn chúng vào dữ liệu seed khiến mọi lần xem thử dữ liệu sau đó
# đều phải tự lọc trong đầu.
DB_TEST="xnk_test"

echo "▸ Bảo đảm database $DB_TEST tồn tại"
if ! docker compose exec -T postgres psql -U postgres -tAc \
      "SELECT 1 FROM pg_database WHERE datname = '$DB_TEST'" | grep -q 1; then
  docker compose exec -T postgres createdb -U postgres "$DB_TEST"
  echo "  · đã tạo"
else
  echo "  · đã có"
fi

echo "▸ Build image test"
docker build --file tools/local/test.Dockerfile --tag xnk/test .

# Mạng của compose: project name khai `name: xnk` ở docker-compose.yml.
MANG="xnk_default"

echo "▸ Chạy test"
docker run --rm \
  --network "$MANG" \
  -e "XNK_TEST_DATABASE_URL=postgresql://postgres:xnk-local-dev@postgres:5432/$DB_TEST" \
  -e "TEST_FILTER=$BO_LOC" \
  xnk/test
