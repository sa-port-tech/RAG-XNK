#!/usr/bin/env bash
# Chạy toàn bộ cấu hình phía GitHub theo đúng thứ tự phụ thuộc.
#
# Thứ tự không tuỳ tiện:
#   00 preflight   — hỏng sớm, hỏng rõ, trước khi sửa bất cứ thứ gì
#   01 repo        — visibility phải là public TRƯỚC, vì ruleset và CodeQL miễn phí
#                    dựa vào điều đó
#   02 teams       — phải có trước ruleset, vì "require code owner review" cần team
#                    trong CODEOWNERS tồn tại thật
#   03 labels      — phải có trước khi ai đó mở issue đầu tiên
#   04 project     — sinh ra PROJECT_NUMBER mà workflow cần
#   05 environment — phải có trước ruleset để cd-deploy không chạy vào môi trường trống
#   06 ruleset     — đặt cuối cùng trong nhóm cấu hình, vì từ lúc này main bị khoá
#   07 actions     — siết quyền sau khi mọi thứ đã chạy được
#   99 verify      — đối chiếu lại toàn bộ
#
# Điều kiện tiên quyết: file .github/ đã có trên nhánh main. Đặt ruleset trước khi
# đẩy file lên sẽ tự khoá chính mình ra ngoài.

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

SCRIPTS=(
  00-preflight.sh
  01-repo-settings.sh
  02-teams.sh
  03-labels.sh
  04-project.sh
  05-environments.sh
  06-ruleset.sh
  07-actions-security.sh
)

ONLY="${1:-}"
if [ -n "$ONLY" ]; then
  SCRIPTS=("$ONLY")
  info "Chỉ chạy: $ONLY"
fi

printf '%s╔══════════════════════════════════════════════════════════╗%s\n' "$C_BOLD" "$C_RESET"
printf '%s║  Cấu hình GitHub cho %-36s║%s\n' "$C_BOLD" "$SLUG" "$C_RESET"
printf '%s╚══════════════════════════════════════════════════════════╝%s\n' "$C_BOLD" "$C_RESET"

FAILED=()
for script in "${SCRIPTS[@]}"; do
  printf '\n%s━━━ %s %s%s\n' "$C_BOLD" "$script" "$(printf '━%.0s' $(seq 1 $((50 - ${#script}))))" "$C_RESET"
  if bash "$SETUP_DIR/$script"; then
    :
  else
    FAILED+=("$script")
    warn "$script kết thúc với lỗi — tiếp tục để thấy hết vấn đề một lượt."
  fi
done

printf '\n%s━━━ 99-verify.sh ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━%s\n' "$C_BOLD" "$C_RESET"
bash "$SETUP_DIR/99-verify.sh" || FAILED+=("99-verify.sh")

printf '\n'
if [ ${#FAILED[@]} -eq 0 ]; then
  printf '%s✓ Toàn bộ cấu hình đã áp và đã đối chiếu lại.%s\n' "$C_GREEN$C_BOLD" "$C_RESET"
else
  printf '%s✗ Còn vấn đề ở: %s%s\n' "$C_RED$C_BOLD" "${FAILED[*]}" "$C_RESET"
  printf '  Đọc lại phần đỏ ở trên. Chạy lại từng script riêng lẻ:\n'
  printf '  bash .github/setup/apply-all.sh <tên-script>\n'
  exit 1
fi
