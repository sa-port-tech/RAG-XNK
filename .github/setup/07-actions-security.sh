#!/usr/bin/env bash
# Siết quyền và phạm vi của GitHub Actions.
#
# Vì sao quan trọng hơn vẻ ngoài của nó: workflow chạy với GITHUB_TOKEN, và token đó
# mặc định có quyền GHI trên toàn repo. Một action bên thứ ba bị chiếm quyền — hoặc
# một PR từ fork chạy được — là đủ để sửa mã, sửa chính workflow, hoặc rút secret.
#
# Ba lớp đặt ở đây:
#   1. GITHUB_TOKEN mặc định chỉ đọc; workflow nào cần ghi thì tự khai báo trong file
#      của nó (mọi workflow trong repo này đều đã khai báo tường minh).
#   2. Chỉ cho chạy action của GitHub, của nhà phát hành đã xác minh, và một danh
#      sách trắng hẹp — thay vì "mọi action trên Marketplace".
#   3. PR từ người ngoài phải được duyệt trước khi workflow chạy.

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
require_gh
require_jq
confirm_repo

step "Quyền mặc định của GITHUB_TOKEN"
gh api -X PUT "repos/$SLUG/actions/permissions/workflow" \
  -f default_workflow_permissions=read \
  -F can_approve_pull_request_reviews=false \
  --silent
ok "Mặc định chỉ đọc · Actions không tự duyệt PR được"

step "Giới hạn action được phép chạy"
gh api -X PUT "repos/$SLUG/actions/permissions" \
  -F enabled=true \
  -f allowed_actions=selected \
  --silent

# Danh sách trắng: đúng những action mà 9 workflow trong repo này dùng, không hơn.
# Thêm action mới là một quyết định có ý thức, phải sửa file này và qua review.
gh api -X PUT "repos/$SLUG/actions/permissions/selected-actions" \
  -F github_owned_allowed=true \
  -F verified_allowed=true \
  --raw-field 'patterns_allowed=[
    "dorny/paths-filter@*",
    "astral-sh/setup-uv@*",
    "peter-evans/find-comment@*",
    "peter-evans/create-or-update-comment@*",
    "peter-evans/create-pull-request@*"
  ]' \
  --silent
ok "Chỉ action của GitHub + nhà phát hành đã xác minh + 5 action trong danh sách trắng"
info "aws-actions/* và docker/* thuộc nhóm nhà phát hành đã xác minh."

step "Workflow chạy từ pull request của người ngoài"
gh api -X PUT "repos/$SLUG/actions/permissions/access" \
  -f access_level=organization \
  --silent 2>/dev/null && ok "Workflow dùng chung trong phạm vi org" || info "Bỏ qua (chỉ áp dụng cho repo riêng tư)"

# Repo public ai cũng fork được. Không có bước duyệt, một PR từ người lạ đã chạy được
# workflow trong ngữ cảnh repo này.
gh api -X PUT "repos/$SLUG/actions/permissions/fork-pr-contributor-approval" \
  -f approval_policy=all_external_contributors \
  --silent 2>/dev/null \
  && ok "Mọi PR từ người ngoài đều cần duyệt trước khi chạy workflow" \
  || warn "Không đặt được qua API — bật tay ở Settings → Actions → General →
     Fork pull request workflows from outside collaborators → Require approval for all external contributors."

step "Thời gian giữ artifact và log"
gh api -X PATCH "repos/$SLUG/actions/permissions/artifact-and-log-retention" \
  -F days=90 --silent 2>/dev/null \
  && ok "90 ngày" \
  || info "Giữ mặc định (đặt ở Settings → Actions → General nếu cần)."

printf '\n%s07-actions-security xong.%s\n' "$C_GREEN" "$C_RESET"
