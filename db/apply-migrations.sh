#!/usr/bin/env bash
#
# Áp toàn bộ script trong db/migrations/ lên một PostgreSQL.
#
# Vì sao tồn tại thay vì gọi `dotnet ef database update`: ba service Python và mọi công cụ
# vận hành đều cần đúng lược đồ này, mà không nên phải cài SDK .NET để có nó. EF Core sở
# hữu lược đồ, nhưng **file .sql mới là hợp đồng liên ngôn ngữ** (ADR-0002).
#
# Script sinh ra ở chế độ idempotent nên chạy lại nhiều lần là an toàn.
#
# Dùng:
#   bash db/apply-migrations.sh                    # vào Postgres của docker-compose
#   DATABASE_URL=postgres://... bash db/apply-migrations.sh   # vào một database khác

set -euo pipefail

MIGRATIONS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/migrations"

if [ ! -d "$MIGRATIONS_DIR" ]; then
  echo "✗ Không tìm thấy $MIGRATIONS_DIR" >&2
  exit 1
fi

# `ls | grep -c` trả mã khác 0 khi rỗng, mà rỗng ở đây là một trạng thái hợp lệ cần báo
# rõ chứ không phải lỗi để `set -e` giết script.
count=$(find "$MIGRATIONS_DIR" -maxdepth 1 -name '*.sql' | wc -l | tr -d ' ')
if [ "$count" = "0" ]; then
  echo "Không có file .sql nào trong db/migrations/ — chưa có gì để áp."
  exit 0
fi

if [ -n "${DATABASE_URL:-}" ]; then
  echo "▸ Áp $count migration vào DATABASE_URL"
  for f in "$MIGRATIONS_DIR"/*.sql; do
    echo "  · $(basename "$f")"
    psql "$DATABASE_URL" --quiet --set ON_ERROR_STOP=1 --file "$f"
  done
else
  echo "▸ Áp $count migration vào Postgres của docker-compose"
  # Chạy psql BÊN TRONG container để không bắt developer phải cài client psql trên máy.
  for f in "$MIGRATIONS_DIR"/*.sql; do
    echo "  · $(basename "$f")"
    docker compose exec -T postgres \
      psql --username postgres --dbname xnk --quiet --set ON_ERROR_STOP=1 < "$f"
  done
fi

echo "✓ Xong."
