#!/usr/bin/env bash
# Tạo GitHub Environment dev · staging · production (docs/00 §8.5).
#
# Environment là chỗ duy nhất trong toàn hệ thống mà luật duyệt deploy có thể sống mà
# không bị người viết workflow vô hiệu hoá. Luật nằm ở phía GitHub, không nằm trong
# repo — nên "không có đường deploy thẳng lên production mà không qua người duyệt,
# kể cả khi ai đó sửa được workflow file" mới là một câu đúng.
#
# Cặp đôi khoá chặt production gồm hai nửa, thiếu nửa nào cũng hỏng:
#   · nửa GitHub  — environment production có required reviewers (script này)
#   · nửa AWS     — trust policy của IAM role chỉ nhận sub `environment:production`
#                   (infra/oidc/, xem docs/17 §5)

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
require_gh
require_jq
confirm_repo

ENV_FILE="$SETUP_DIR/data/environments.json"

team_id() {
  api "orgs/$ORG/teams/$1" --jq .id 2>/dev/null || echo ""
}

for name in $(jq -r '.[].name' "$ENV_FILE"); do
  entry=$(jq -c --arg n "$name" '.[] | select(.name == $n)' "$ENV_FILE")

  step "Môi trường: $name"
  info "$(echo "$entry" | jq -r .comment)"

  # ── Người duyệt ────────────────────────────────────────────────────────────
  reviewers="[]"
  missing_team=false
  while read -r slug; do
    [ -z "$slug" ] && continue
    id=$(team_id "$slug")
    if [ -z "$id" ]; then
      problem "Team '$slug' chưa tồn tại — chạy 02-teams.sh trước."
      missing_team=true
      continue
    fi
    reviewers=$(echo "$reviewers" | jq -c --argjson id "$id" '. + [{type:"Team", id:$id}]')
  done < <(echo "$entry" | jq -r '.reviewer_teams[]?')

  $missing_team && continue

  wait_timer=$(echo "$entry" | jq -r .wait_timer)
  prevent_self=$(echo "$entry" | jq -r .prevent_self_review)

  # `-F` và `--raw-field` gửi mọi giá trị dưới dạng chuỗi, nên GitHub từ chối
  # reviewers và deployment_branch_policy với lỗi "is not of type array/object".
  # Trường lồng nhau phải đi trong body JSON thật.
  jq -n \
    --argjson wait "$wait_timer" \
    --argjson self "$prevent_self" \
    --argjson reviewers "$reviewers" \
    '{
      wait_timer: $wait,
      prevent_self_review: $self,
      reviewers: $reviewers,
      deployment_branch_policy: {
        protected_branches: false,
        custom_branch_policies: true
      }
    }' | gh api -X PUT "repos/$SLUG/environments/$name" --input - --silent

  reviewer_count=$(echo "$reviewers" | jq 'length')
  if [ "$reviewer_count" -gt 0 ]; then
    ok "required reviewers: $(echo "$entry" | jq -r '.reviewer_teams | join(", ")') · chờ ${wait_timer} phút · cấm tự duyệt"
  else
    ok "không cần duyệt · deploy tự động"
  fi

  # ── Nhánh được phép deploy ─────────────────────────────────────────────────
  existing=$(api "repos/$SLUG/environments/$name/deployment-branch-policies" --jq '.branch_policies[].name' || true)
  while read -r branch; do
    [ -z "$branch" ] && continue
    if echo "$existing" | grep -qx "$branch"; then
      info "nhánh được phép: $branch (đã có)"
    else
      gh api -X POST "repos/$SLUG/environments/$name/deployment-branch-policies" \
        -f name="$branch" -f type=branch --silent
      ok "chỉ deploy được từ nhánh: $branch"
    fi
  done < <(echo "$entry" | jq -r '.branch_policies[]?')

  # ── Biến môi trường đã biết ────────────────────────────────────────────────
  while read -r key value; do
    key=$(echo "$key" | base64 -d)
    value=$(echo "$value" | base64 -d)
    gh variable set "$key" --repo "$SLUG" --env "$name" --body "$value"
    ok "vars.$key = $value"
  done < <(echo "$entry" | jq -r '.variables | to_entries[] | [(.key|@base64), (.value|@base64)] | @tsv')

  # ── Biến chưa có giá trị ───────────────────────────────────────────────────
  # Cố ý KHÔNG tạo biến rỗng: biến rỗng khiến cd-deploy hỏng ở giữa chừng với thông
  # báo khó hiểu, trong khi biến thiếu hẳn thì hỏng ngay từ bước đầu và nói rõ tên.
  while read -r key hint; do
    key=$(echo "$key" | base64 -d)
    hint=$(echo "$hint" | base64 -d)
    if gh variable list --repo "$SLUG" --env "$name" --json name --jq '.[].name' | grep -qx "$key"; then
      info "vars.$key — đã đặt"
    else
      warn "CHƯA ĐẶT vars.$key — $hint"
      info "    gh variable set $key --repo $SLUG --env $name --body '<giá trị>'"
    fi
  done < <(echo "$entry" | jq -r '.pending_variables // {} | to_entries[] | [(.key|@base64), (.value|@base64)] | @tsv')
done

step "Kiểm tra không có access key AWS nào"
# docs/09 E1-07 AC đầu tiên: "Không có AWS_ACCESS_KEY_ID nào trong GitHub Secrets —
# verify bằng cách liệt kê secrets."
FOUND=$(gh secret list --repo "$SLUG" --json name --jq '.[].name' 2>/dev/null | grep -iE 'AWS_(ACCESS_KEY|SECRET)' || true)
if [ -n "$FOUND" ]; then
  problem "Tìm thấy secret dạng access key AWS: $FOUND
     Đây là vi phạm trực tiếp docs/00 §8.5. Xoá nó và dùng OIDC federation.
     Sau khi xoá, PHẢI vô hiệu hoá luôn access key đó trong IAM — nó đã tồn tại."
else
  ok "Không có secret nào dạng AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY"
fi

exit $PROBLEMS
