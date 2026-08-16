#!/usr/bin/env bash
# Smoke test sau khi deploy — gọi healthcheck của từng service qua ALB.
#
# Mục đích hẹp và cố ý: trả lời "bản vừa deploy có sống không", không phải "bản vừa
# deploy có đúng không". Kiểm thử chức năng là việc của test tự động và của eval.
#
# Chạy được cả trên máy cá nhân:
#   BASE_URL=https://dev.example.internal .github/scripts/smoke_test.sh
#   BASE_URL=... SERVICES="chat retrieval" .github/scripts/smoke_test.sh

set -uo pipefail

BASE_URL="${BASE_URL:?BASE_URL chưa được đặt (biến môi trường của GitHub Environment)}"
MANIFEST="${MANIFEST:-.github/services.json}"
ATTEMPTS="${ATTEMPTS:-12}"
SLEEP_SECONDS="${SLEEP_SECONDS:-10}"
TIMEOUT_SECONDS="${TIMEOUT_SECONDS:-10}"

BASE_URL="${BASE_URL%/}"

if [ ! -f "$MANIFEST" ]; then
  echo "::error::Không tìm thấy danh mục service '$MANIFEST'."
  exit 1
fi

# Mặc định kiểm tra mọi service; truyền SERVICES để thu hẹp về những service vừa deploy.
if [ -n "${SERVICES:-}" ]; then
  read -r -a wanted <<< "$SERVICES"
else
  mapfile -t wanted < <(jq -r '.services[].name' "$MANIFEST")
fi

echo "Smoke test trên $BASE_URL"
echo "Service: ${wanted[*]}"
echo

failed=()

for name in "${wanted[@]}"; do
  health_path=$(jq -r --arg n "$name" '.services[] | select(.name == $n) | .health_path' "$MANIFEST")

  if [ -z "$health_path" ] || [ "$health_path" = "null" ]; then
    echo "::error::Service '$name' không có trong $MANIFEST hoặc thiếu health_path."
    failed+=("$name")
    continue
  fi

  url="${BASE_URL}/${name}${health_path}"
  echo "→ $name  $url"

  ok=false
  for attempt in $(seq 1 "$ATTEMPTS"); do
    # ECS rolling update cần thời gian để target group chuyển sang healthy; thử lại
    # là hành vi đúng ở đây, không phải che giấu lỗi.
    status=$(curl --silent --show-error --output /dev/null \
                  --write-out '%{http_code}' \
                  --max-time "$TIMEOUT_SECONDS" \
                  "$url" 2>/dev/null || echo "000")

    if [ "$status" = "200" ]; then
      echo "   ✅ HTTP 200 (lần thử $attempt)"
      ok=true
      break
    fi

    echo "   … HTTP $status (lần thử $attempt/$ATTEMPTS)"
    [ "$attempt" -lt "$ATTEMPTS" ] && sleep "$SLEEP_SECONDS"
  done

  if [ "$ok" != true ]; then
    echo "::error title=smoke-test::$name không trả về HTTP 200 tại $url sau $ATTEMPTS lần thử."
    failed+=("$name")
  fi
done

echo
if [ ${#failed[@]} -gt 0 ]; then
  echo "::error::Smoke test thất bại: ${failed[*]}"
  exit 1
fi

echo "Smoke test đạt cho toàn bộ ${#wanted[@]} service."
