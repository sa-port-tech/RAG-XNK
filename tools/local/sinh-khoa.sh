#!/usr/bin/env bash
#
# Sinh khoá ký JWT riêng cho máy này và ghi vào `.env`.
#
#   bash tools/local/sinh-khoa.sh
#
# Vì sao có script này thay vì một giá trị mẫu dùng được luôn:
#
# `.env.example` từng mang một khoá dài 51 ký tự, qua mọi phép đo độ dài, và header của
# file có dặn "chép rồi sửa theo máy mình". Đo ngày 08/09/2026: `.env` trên máy dev mang
# **y nguyên** giá trị đó. Không ai cố tình bỏ qua bước sửa — chỉ là bỏ qua nó không gây
# ra hậu quả nào nhìn thấy được, nên nó không bao giờ được làm.
#
# Một khoá nằm trong git thì bất kỳ ai đọc lịch sử repo cũng ký được token hợp lệ cho
# **mọi** service, vì prototype dùng chung một khoá đối xứng (ADR-014). Nên giá trị mẫu
# bây giờ cố tình KHÔNG dùng được, và đây là đường để có một khoá dùng được.
#
# Chạy lại được nhiều lần: mỗi lần sinh một khoá mới và thay dòng cũ. Sau khi đổi khoá,
# mọi token đã phát trở thành không hợp lệ — đăng nhập lại là xong.

set -euo pipefail

GOC="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
TEP_ENV="$GOC/.env"
TEP_MAU="$GOC/.env.example"
BIEN="XNK_JWT_SIGNING_KEY"

# ─── Sinh khoá ───────────────────────────────────────────────────────────────────
# 36 byte ngẫu nhiên → 48 ký tự base64, thừa xa ngưỡng 32 byte của HMAC-SHA256.
#
# Ba đường vì không máy nào chắc chắn có cả ba: openssl có sẵn trong Git Bash trên
# Windows và trong hầu hết ảnh Linux; /dev/urandom có trên Unix; python3 là lưới cuối.
sinh_khoa() {
    if command -v openssl >/dev/null 2>&1; then
        openssl rand -base64 36 | tr -d '\n'
    elif [ -r /dev/urandom ]; then
        head -c 36 /dev/urandom | base64 | tr -d '\n'
    elif command -v python3 >/dev/null 2>&1; then
        python3 -c 'import base64,secrets;print(base64.b64encode(secrets.token_bytes(36)).decode(),end="")'
    else
        echo "Không tìm thấy openssl, /dev/urandom hay python3 để sinh khoá ngẫu nhiên." >&2
        echo "Cài một trong ba, hoặc tự đặt $BIEN (≥32 byte) trong .env." >&2
        exit 1
    fi
}

# ─── Bảo đảm có .env ─────────────────────────────────────────────────────────────
if [ ! -f "$TEP_ENV" ]; then
    if [ ! -f "$TEP_MAU" ]; then
        echo "Không thấy $TEP_MAU — chạy script này từ trong repo." >&2
        exit 1
    fi
    cp "$TEP_MAU" "$TEP_ENV"
    echo "Đã tạo .env từ .env.example."
fi

KHOA="$(sinh_khoa)"

# ─── Thay dòng khoá ──────────────────────────────────────────────────────────────
# Ghi qua file tạm rồi thay chỗ, không dùng `sed -i`: `sed -i` trên Git Bash/macOS đòi
# cú pháp khác nhau cho tham số hậu tố, và một trong hai sẽ để lại file `.env''` rác.
#
# Không dùng `sed s|...|$KHOA|` cho phần thay thế: khoá base64 chứa `/` và có thể chứa
# `&`, cả hai đều có nghĩa đặc biệt với sed. Ghép chuỗi bằng awk theo biến thì không.
TAM="$(mktemp)"
trap 'rm -f "$TAM"' EXIT

if grep -q "^${BIEN}=" "$TEP_ENV"; then
    awk -v bien="$BIEN" -v khoa="$KHOA" \
        'index($0, bien "=") == 1 { print bien "=" khoa; next } { print }' \
        "$TEP_ENV" >"$TAM"
else
    cp "$TEP_ENV" "$TAM"
    printf '\n%s=%s\n' "$BIEN" "$KHOA" >>"$TAM"
fi

cat "$TAM" >"$TEP_ENV"

echo "Đã ghi $BIEN mới vào .env (${#KHOA} ký tự)."
echo "Khoá KHÔNG in ra đây — mở .env nếu cần xem."
echo
echo "Service đang chạy thì khởi động lại để nhận khoá mới:"
echo "  docker compose --profile app up -d --force-recreate"
