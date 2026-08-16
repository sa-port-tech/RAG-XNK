#!/usr/bin/env bash
# Tạo 9 team của org và cấp quyền trên repo.
#
# Team phải tồn tại VÀ có quyền write thì CODEOWNERS mới có tác dụng. Nếu thiếu một
# trong hai, GitHub bỏ qua dòng CODEOWNERS đó trong im lặng — không báo lỗi, không
# gán reviewer, và không ai phát hiện ra cho tới khi có sự cố. Script này kiểm tra
# lại cả hai điều kiện sau khi tạo.

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
require_gh
require_jq
confirm_repo

TEAMS_FILE="$SETUP_DIR/data/teams.json"

step "Tạo team trong org $ORG"

while read -r slug name description permission; do
  slug=$(echo "$slug" | base64 -d)
  name=$(echo "$name" | base64 -d)
  description=$(echo "$description" | base64 -d)
  permission=$(echo "$permission" | base64 -d)

  if exists "orgs/$ORG/teams/$slug"; then
    info "$slug — đã có"
  else
    gh api -X POST "orgs/$ORG/teams" \
      -f name="$name" \
      -f description="$description" \
      -f privacy=closed \
      -f notification_setting=notifications_enabled \
      --silent
    ok "$slug — đã tạo"
  fi

  # Idempotent: PUT lặp lại chỉ đặt lại cùng mức quyền.
  gh api -X PUT "orgs/$ORG/teams/$slug/repos/$SLUG" -f permission="$permission" --silent
  info "  quyền trên repo: $permission"
done < <(jq -r '.[] | [(.slug|@base64), (.name|@base64), (.description|@base64), (.permission|@base64)] | @tsv' "$TEAMS_FILE")

step "Đối chiếu với CODEOWNERS"
CODEOWNERS_FILE="$REPO_ROOT/.github/CODEOWNERS"
if [ ! -f "$CODEOWNERS_FILE" ]; then
  warn "Không tìm thấy .github/CODEOWNERS"
else
  # Rút mọi handle dạng @org/team ra khỏi CODEOWNERS và kiểm tra từng cái có thật.
  MISSING=0
  while read -r handle; do
    team="${handle#@$ORG/}"
    if exists "orgs/$ORG/teams/$team"; then
      ok "$handle"
    else
      problem "$handle xuất hiện trong CODEOWNERS nhưng team không tồn tại — GitHub sẽ bỏ qua dòng đó."
      MISSING=1
    fi
  done < <(grep -oE "@$ORG/[a-z0-9-]+" "$CODEOWNERS_FILE" | sort -u)

  [ "$MISSING" -eq 0 ] && ok "Mọi team trong CODEOWNERS đều tồn tại"
fi

step "Nhắc việc còn lại cho con người"
info "Script không thêm được thành viên vào team — cần biết username GitHub thật."
info "Thêm bằng:  gh api -X PUT orgs/$ORG/teams/<team>/memberships/<username> -f role=member"
info "Đặc biệt quan trọng: @$ORG/xnk-expert-team phải có đủ 2 chuyên gia,"
info "vì họ là code owner của prompts/, eval/golden-set/ và bpmn/."

exit $PROBLEMS
