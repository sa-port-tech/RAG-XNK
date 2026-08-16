#!/usr/bin/env bash
# Kiểm tra điều kiện cần trước khi thay đổi bất cứ thứ gì trên GitHub.
#
# Chạy trước để hỏng sớm và hỏng rõ, thay vì hỏng ở script thứ năm khi đã sửa được
# một nửa cấu hình.

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

step "Kiểm tra GitHub CLI"
require_gh
info "$(gh --version | head -1)"
ok "Đã đăng nhập: $(gh api user --jq .login)"

step "Kiểm tra phạm vi quyền của token"
# admin:org cần cho việc tạo team; project cần cho Projects v2; workflow cần để đẩy
# thay đổi vào .github/workflows/.
SCOPES=$(gh auth status 2>&1 | grep -i 'Token scopes' || true)
info "${SCOPES:-không đọc được scope}"
for scope in admin:org project workflow repo; do
  if ! echo "$SCOPES" | grep -q "'$scope'"; then
    warn "Thiếu scope '$scope'. Bổ sung bằng: gh auth refresh -h github.com -s $scope"
  else
    ok "scope $scope"
  fi
done

step "Kiểm tra tổ chức $ORG"
if exists "orgs/$ORG"; then
  PLAN=$(api "orgs/$ORG" --jq '.plan.name // "không đọc được"')
  ok "Tổ chức tồn tại (gói: $PLAN)"
else
  die "Không tìm thấy tổ chức '$ORG'.
     GitHub KHÔNG có API tạo tổ chức — phải tạo bằng tay:
     https://github.com/organizations/plan → chọn Free → đặt tên '$ORG'"
fi

step "Kiểm tra repo $SLUG"
if exists "repos/$SLUG"; then
  CURRENT_VISIBILITY=$(api "repos/$SLUG" --jq .visibility)
  ok "Repo tồn tại (visibility: $CURRENT_VISIBILITY)"
  if [ "$CURRENT_VISIBILITY" != "$VISIBILITY" ]; then
    info "Sẽ đổi sang '$VISIBILITY' ở bước 01."
  fi
else
  warn "Chưa có $SLUG."
  info "Chuyển repo cá nhân sang org bằng lệnh:"
  info "  gh api -X POST repos/<user>/$REPO/transfer -f new_owner=$ORG"
  info "Hoặc chuyển trong Settings → General → Transfer ownership."
  die "Dừng lại — các script sau cần repo đã nằm trong org."
fi

step "Kiểm tra quyền quản trị trên repo"
if [ "$(api "repos/$SLUG" --jq '.permissions.admin')" = "true" ]; then
  ok "Có quyền admin"
else
  die "Tài khoản hiện tại không có quyền admin trên $SLUG — không đặt được ruleset và environment."
fi

step "Kiểm tra tính năng phụ thuộc vào gói và visibility"
if [ "$VISIBILITY" = "public" ]; then
  ok "Repo public → Rulesets, CodeQL, Environment protection đều dùng được miễn phí"
else
  warn "Repo $VISIBILITY: Rulesets và Environment protection cần gói Team trở lên;
     CodeQL cần GitHub Advanced Security. Xem docs/17 §1."
fi

printf '\n%sPreflight đạt. Chạy tiếp: .github/setup/apply-all.sh%s\n' "$C_GREEN" "$C_RESET"
