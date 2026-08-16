#!/usr/bin/env bash
# Ruleset bảo vệ nhánh main (docs/00 §8.4).
#
#   Cấm push trực tiếp · bắt buộc PR và status check · linear history (squash merge)
#   · giải quyết hết comment trước khi merge · signed commits · cấm force push và
#   xoá nhánh.
#
# Payload được dựng từ .github/setup/config.env chứ không viết cứng ở đây, để danh
# sách required check chỉ tồn tại ở đúng một chỗ. Bản đã dựng được ghi ra
# .github/setup/data/ruleset-main.rendered.json để review và để đối chiếu về sau.
#
# Một điểm dễ sai: `strict_required_status_checks_policy: true` bắt nhánh phải cập
# nhật với main trước khi merge. Nếu không bật, hai PR xanh độc lập vẫn có thể hợp
# lại thành một main đỏ — đúng kiểu lỗi chỉ xuất hiện sau khi đã merge.

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
require_gh
require_jq
confirm_repo

RULESET_NAME="main — bảo vệ nhánh"
RENDERED="$SETUP_DIR/data/ruleset-main.rendered.json"

step "Dựng payload ruleset"

CHECKS_JSON=$(printf '%s\n' "${REQUIRED_CHECKS[@]}" | jq -R . | jq -s 'map({context: .})')
info "Required status check: ${REQUIRED_CHECKS[*]}"

BYPASS_JSON='[]'
if [ "$BOOTSTRAP_ADMIN_BYPASS" = "true" ]; then
  # bypass_mode "pull_request" nghĩa là chủ sở hữu org bỏ qua được luật KHI MERGE PR,
  # nhưng vẫn KHÔNG push thẳng vào main được. Đây là lối thoát hẹp nhất đủ dùng cho
  # giai đoạn org mới có một người.
  BYPASS_JSON='[{"actor_id":1,"actor_type":"OrganizationAdmin","bypass_mode":"pull_request"}]'
  warn "BOOTSTRAP_ADMIN_BYPASS=true — chủ sở hữu org bỏ qua được luật khi merge PR."
  warn "Đặt lại thành false trong config.env ngay khi có người thứ hai trong org."
fi

RULES=$(jq -n \
  --argjson checks "$CHECKS_JSON" \
  --argjson approvals "$REQUIRED_APPROVALS" \
  --argjson last_push "$REQUIRE_LAST_PUSH_APPROVAL" \
  '[
    { "type": "deletion" },
    { "type": "non_fast_forward" },
    { "type": "required_linear_history" },
    {
      "type": "pull_request",
      "parameters": {
        "required_approving_review_count": $approvals,
        "dismiss_stale_reviews_on_push": true,
        "require_code_owner_review": true,
        "require_last_push_approval": $last_push,
        "required_review_thread_resolution": true
      }
    },
    {
      "type": "required_status_checks",
      "parameters": {
        "strict_required_status_checks_policy": true,
        "do_not_enforce_on_create": false,
        "required_status_checks": $checks
      }
    }
  ]')

if [ "$REQUIRE_SIGNED_COMMITS" = "true" ]; then
  RULES=$(echo "$RULES" | jq '. + [{"type":"required_signatures"}]')
  info "Bắt buộc ký commit — thành viên phải cấu hình GPG/SSH signing trước (docs/17 §3)."
fi

jq -n \
  --arg name "$RULESET_NAME" \
  --argjson bypass "$BYPASS_JSON" \
  --argjson rules "$RULES" \
  '{
    name: $name,
    target: "branch",
    enforcement: "active",
    conditions: { ref_name: { include: ["~DEFAULT_BRANCH"], exclude: [] } },
    bypass_actors: $bypass,
    rules: $rules
  }' > "$RENDERED"

ok "Đã ghi $RENDERED"

step "Áp ruleset lên $SLUG"
EXISTING_ID=$(api "repos/$SLUG/rulesets" | jq -r --arg n "$RULESET_NAME" '.[] | select(.name == $n) | .id' | head -1)

if [ -n "$EXISTING_ID" ]; then
  gh api -X PUT "repos/$SLUG/rulesets/$EXISTING_ID" --input "$RENDERED" --silent
  ok "Đã cập nhật ruleset #$EXISTING_ID"
else
  NEW_ID=$(gh api -X POST "repos/$SLUG/rulesets" --input "$RENDERED" --jq .id)
  ok "Đã tạo ruleset #$NEW_ID"
fi

step "Đọc lại từ GitHub để xác nhận"
api "repos/$SLUG/rulesets" \
  | jq -r --arg n "$RULESET_NAME" '.[] | select(.name == $n) | to_entries[]
      | select(.key | IN("id","enforcement","target")) | "  \(.key): \(.value)"'

ACTIVE_RULES=$(api "repos/$SLUG/rules/branches/$DEFAULT_BRANCH" --jq '[.[].type] | unique | join(", ")')
info "Luật đang có hiệu lực trên $DEFAULT_BRANCH: $ACTIVE_RULES"

for needed in deletion non_fast_forward required_linear_history pull_request required_status_checks; do
  echo "$ACTIVE_RULES" | grep -q "$needed" \
    && ok "$needed" \
    || problem "Luật '$needed' không có hiệu lực trên $DEFAULT_BRANCH."
done

step "Lưu ý về required status check"
info "GitHub chỉ nhận diện một check sau khi nó đã chạy ít nhất một lần trên repo."
info "Nếu check nào hiển thị 'Expected — Waiting for status', hãy mở một PR nháp để"
info "toàn bộ workflow chạy một lượt, rồi chạy lại 99-verify.sh."

exit $PROBLEMS
