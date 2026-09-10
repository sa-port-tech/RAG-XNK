#!/usr/bin/env bash
#
# Xuất script SQL idempotent của một EF Core migration vào db/migrations/.
#
# ADR-010: EF Core sở hữu lược đồ, nhưng file `.sql` mới là hợp đồng liên ngôn ngữ. Sinh
# migration xong mà quên xuất `.sql` thì lược đồ và hợp đồng lệch nhau, và ba service
# Python cùng task migration trên ECS đều đang đọc bản cũ.
#
# Dùng:
#   bash tools/local/xuat-migration.sh <Project> <DbContext> [ten-file-khong-duoi]
#
# Ví dụ:
#   bash tools/local/xuat-migration.sh Xnk.IdentityTenant IdentityDbContext
#   bash tools/local/xuat-migration.sh Xnk.IdentityTenant IdentityDbContext 0003_ten_khac
#
# ── Vì sao tên file được SINH RA khi không truyền ─────────────────────────────────────
# Bỏ trống thì script tự đặt tên từ migration MỚI NHẤT của project:
#
#   20260909165231_EmailChuThuong  ->  0003_email_chu_thuong.sql
#
# Số thứ tự đếm từ số file .sql đã có, phần tên chuyển từ PascalCase sang snake_case.
#
# Trước đây tham số này bắt buộc và gõ tay, nên hai hệ đánh số — `0002_initial_identity`
# và `20260902094756_InitialIdentity` — chỉ dính với nhau nhờ trí nhớ của người chạy lệnh.
# Gõ nhầm số thì `apply-migrations.sh` chạy sai thứ tự (nó sắp theo tên file), và triệu
# chứng là một migration áp trước cái nó phụ thuộc.
#
# Chạy trong container — xem tools/local/ef-script.Dockerfile để biết vì sao.

set -euo pipefail

if [ "$#" -lt 2 ] || [ "$#" -gt 3 ]; then
  echo "Dùng: bash tools/local/xuat-migration.sh <Project> <DbContext> [ten-file]" >&2
  exit 1
fi

PROJECT="$1"
CONTEXT="$2"
OUTPUT="${3:-}"

GOC_TAM="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

if [ -z "$OUTPUT" ]; then
  # Migration mới nhất = file .cs có timestamp lớn nhất, bỏ .Designer.cs và snapshot.
  moi_nhat=$(find "$GOC_TAM/src/dotnet/$PROJECT/Data/Migrations" -maxdepth 1 -name '[0-9]*_*.cs'              ! -name '*.Designer.cs' -printf '%f
' 2>/dev/null | sort | tail -1)

  if [ -z "$moi_nhat" ]; then
    echo "Không tìm thấy migration nào trong src/dotnet/$PROJECT/Data/Migrations." >&2
    echo "Chạy 'dotnet ef migrations add <Ten>' trước, hoặc truyền tên file làm tham số 3." >&2
    exit 1
  fi

  # 20260909165231_EmailChuThuong.cs -> EmailChuThuong -> email_chu_thuong
  ten_pascal="${moi_nhat#*_}"
  ten_pascal="${ten_pascal%.cs}"
  ten_snake=$(printf '%s' "$ten_pascal" | sed -E 's/([a-z0-9])([A-Z])/_/g' | tr '[:upper:]' '[:lower:]')

  so_hien_co=$(find "$GOC_TAM/db/migrations" -maxdepth 1 -name '*.sql' | wc -l | tr -d ' ')
  OUTPUT=$(printf '%04d_%s' "$((so_hien_co + 1))" "$ten_snake")

  echo "▸ Tên file sinh từ migration mới nhất ($moi_nhat): $OUTPUT.sql"
fi

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
