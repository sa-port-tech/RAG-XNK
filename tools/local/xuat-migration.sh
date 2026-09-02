#!/usr/bin/env bash
#
# Xuất script SQL idempotent của một EF Core migration vào db/migrations/.
#
# ADR-010: EF Core sở hữu lược đồ, nhưng file `.sql` mới là hợp đồng liên ngôn ngữ. Sinh
# migration xong mà quên xuất `.sql` thì lược đồ và hợp đồng lệch nhau, và ba service
# Python cùng task migration trên ECS đều đang đọc bản cũ.
#
# Dùng:
#   bash tools/local/xuat-migration.sh <Project> <DbContext> <ten-file-khong-duoi>
#
# Ví dụ:
#   bash tools/local/xuat-migration.sh Xnk.IdentityTenant IdentityDbContext 0002_initial_identity
#
# Chạy trong container — xem tools/local/ef-script.Dockerfile để biết vì sao.

set -euo pipefail

if [ "$#" -ne 3 ]; then
  echo "Dùng: bash tools/local/xuat-migration.sh <Project> <DbContext> <ten-file>" >&2
  exit 1
fi

PROJECT="$1"
CONTEXT="$2"
OUTPUT="$3"

GOC="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ANH="xnk/ef-script:${OUTPUT}"
# Tên container tạm gắn với tên file để hai lần chạy song song không giẫm lên nhau.
TAM="xnk-ef-script-${OUTPUT}"

cd "$GOC"

echo "▸ Build image xuất migration ($PROJECT / $CONTEXT)"
docker build \
  --file tools/local/ef-script.Dockerfile \
  --build-arg "PROJECT=$PROJECT" \
  --build-arg "CONTEXT=$CONTEXT" \
  --build-arg "OUTPUT=$OUTPUT" \
  --tag "$ANH" \
  .

# `docker cp` chứ không phải bind mount: repo có thể nằm trên ổ ảo (Google Drive) mà Docker
# Desktop không chia sẻ được vào WSL2 — khi đó mount cho ra thư mục RỖNG mà không báo lỗi.
echo "▸ Lấy file ra khỏi image"
docker rm -f "$TAM" >/dev/null 2>&1 || true
docker create --name "$TAM" "$ANH" >/dev/null
docker cp "$TAM:/out/${OUTPUT}.sql" "db/migrations/${OUTPUT}.sql"
docker rm "$TAM" >/dev/null

echo "✓ Đã ghi db/migrations/${OUTPUT}.sql"
echo "  Nhớ commit nó CÙNG PR với migration (ADR-010)."
