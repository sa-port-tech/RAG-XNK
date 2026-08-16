#!/usr/bin/env bash
# Cấu hình repo: visibility, cách merge, tính năng, bảo mật.
#
# Vì sao chỉ cho phép squash merge: docs/00 §8.4 yêu cầu linear history, và §8.3 ⑦
# dựng changelog từ tiêu đề PR. Cả hai chỉ đúng khi mỗi PR thành đúng một commit.

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
require_gh
require_jq
confirm_repo

step "Đặt visibility và tính năng repo"
gh api -X PATCH "repos/$SLUG" \
  -f visibility="$VISIBILITY" \
  -f description="Trợ lý hỏi–đáp pháp luật xuất nhập khẩu trên nền RAG — prototype" \
  -F has_issues=true \
  -F has_projects=true \
  -F has_discussions=true \
  -F has_wiki=false \
  -F allow_squash_merge=true \
  -F allow_merge_commit=false \
  -F allow_rebase_merge=false \
  -F allow_auto_merge=true \
  -F delete_branch_on_merge=true \
  -F allow_update_branch=true \
  -f squash_merge_commit_title=PR_TITLE \
  -f squash_merge_commit_message=PR_BODY \
  --silent
ok "visibility=$VISIBILITY · chỉ squash merge · tự xoá nhánh sau merge"

step "Đặt nhánh mặc định"
CURRENT_DEFAULT=$(api "repos/$SLUG" --jq .default_branch)
if [ "$CURRENT_DEFAULT" = "$DEFAULT_BRANCH" ]; then
  ok "Đã là '$DEFAULT_BRANCH'"
else
  gh api -X PATCH "repos/$SLUG" -f default_branch="$DEFAULT_BRANCH" --silent
  ok "Đổi từ '$CURRENT_DEFAULT' sang '$DEFAULT_BRANCH'"
fi

step "Bật cảnh báo phụ thuộc và sửa lỗi bảo mật tự động"
gh api -X PUT "repos/$SLUG/vulnerability-alerts" --silent && ok "Dependabot alerts"
gh api -X PUT "repos/$SLUG/automated-security-fixes" --silent && ok "Dependabot security updates"

step "Bật quét bí mật và chặn đẩy bí mật"
# Chặn ngay lúc push là điểm khác biệt quan trọng: quét sau khi đã lên repo công khai
# thì bí mật đã lộ rồi, xoay vòng khoá là việc bắt buộc chứ không phải tuỳ chọn.
#
# Không dùng `PATCH repos/{repo}` với `security_and_analysis` — GitHub đã chuyển sang
# mô hình "code security configuration" ở cấp tổ chức và endpoint cũ trả về HTTP 422
# "Invalid security_and_analysis payload" dù payload đúng tài liệu. Cách còn hiệu lực
# là gắn repo vào một configuration của org.
CONFIG_ID=$(api "orgs/$ORG/code-security/configurations" \
  | jq -r '[.[] | select(.secret_scanning == "enabled" and .secret_scanning_push_protection == "enabled")][0].id // empty')

if [ -z "$CONFIG_ID" ]; then
  warn "Org $ORG chưa có code security configuration nào bật secret scanning + push protection."
  info "Tạo tại: https://github.com/organizations/$ORG/settings/security_products"
else
  REPO_ID=$(api "repos/$SLUG" --jq .id)
  gh api -X POST "orgs/$ORG/code-security/configurations/$CONFIG_ID/attach" \
    -f scope=selected -F "selected_repository_ids[]=$REPO_ID" --silent

  # Việc gắn chạy bất đồng bộ; đọc lại để khẳng định thay vì tin vào HTTP 2xx.
  for _ in 1 2 3 4 5 6; do
    STATUS=$(api "repos/$SLUG" --jq '.security_and_analysis.secret_scanning_push_protection.status')
    [ "$STATUS" = "enabled" ] && break
    sleep 3
  done

  if [ "$STATUS" = "enabled" ]; then
    ok "Secret scanning + push protection (configuration #$CONFIG_ID)"
  else
    problem "Đã gắn configuration #$CONFIG_ID nhưng push protection vẫn '$STATUS'."
  fi
fi

step "Bật nhận báo cáo lỗ hổng riêng tư"
gh api -X PUT "repos/$SLUG/private-vulnerability-reporting" --silent \
  && ok "Private vulnerability reporting" \
  || warn "Không bật được — bật tay ở Settings → Advanced Security."

step "Bật GitHub Pages? Không."
info "Bỏ qua — Blazor được phân phối qua S3 + CloudFront (docs/00 §7)."

printf '\n%s01-repo-settings xong.%s\n' "$C_GREEN" "$C_RESET"
