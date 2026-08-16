#!/usr/bin/env bash
# Đối chiếu hiện trạng thật trên GitHub với từng yêu cầu trong tài liệu.
#
# Script này không sửa gì. Nó tồn tại để trả lời một câu hỏi duy nhất: cái gì trong
# docs/00 §8 đã thành hiện thực, và cái gì mới chỉ nằm trên giấy.
#
# Mỗi dòng kiểm tra ghi kèm điều khoản mà nó bảo vệ, để khi đỏ thì biết ngay đang
# hỏng cam kết nào chứ không phải chỉ biết "có gì đó sai".

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
require_gh
require_jq
require_python
confirm_repo

CHECKS=0
pass() { CHECKS=$((CHECKS + 1)); ok "$*"; }
miss() { CHECKS=$((CHECKS + 1)); problem "$*"; }

expect() { # expect <mô tả> <giá trị thực> <giá trị mong đợi>
  if [ "$2" = "$3" ]; then pass "$1 = $3"; else miss "$1 = '$2', mong đợi '$3'"; fi
}

# ── §8.1 — file trong repo ─────────────────────────────────────────────────────
step "§8.1 · Cấu trúc .github/"
for file in \
  ".github/CODEOWNERS" \
  ".github/PULL_REQUEST_TEMPLATE.md" \
  ".github/ISSUE_TEMPLATE/config.yml" \
  ".github/ISSUE_TEMPLATE/bug.yml" \
  ".github/ISSUE_TEMPLATE/feature.yml" \
  ".github/ISSUE_TEMPLATE/corpus-issue.yml" \
  ".github/ISSUE_TEMPLATE/expert-review.yml" \
  ".github/workflows/ci-dotnet.yml" \
  ".github/workflows/ci-python.yml" \
  ".github/workflows/ci-blazor.yml" \
  ".github/workflows/ci-bpmn.yml" \
  ".github/workflows/eval-regression.yml" \
  ".github/workflows/cd-deploy.yml" \
  ".github/workflows/project-automation.yml" \
  ".github/workflows/codeql.yml" \
  ".github/workflows/pr-governance.yml"
do
  if exists "repos/$SLUG/contents/$file"; then pass "$file"; else miss "$file chưa có trên nhánh $DEFAULT_BRANCH"; fi
done

# ── §8.6 — CODEOWNERS ──────────────────────────────────────────────────────────
step "§8.6 · CODEOWNERS"
CO_ERRORS=$(api "repos/$SLUG/codeowners/errors" --jq '.errors | length' 2>/dev/null || echo "?")
if [ "$CO_ERRORS" = "0" ]; then
  pass "GitHub phân tích CODEOWNERS không lỗi"
elif [ "$CO_ERRORS" = "?" ]; then
  miss "Không đọc được kết quả phân tích CODEOWNERS"
else
  miss "CODEOWNERS có $CO_ERRORS lỗi:"
  api "repos/$SLUG/codeowners/errors" --jq '.errors[] | "     dòng \(.line): \(.message)"'
fi

# ── §8.2 — Project board ───────────────────────────────────────────────────────
step "§8.2 · Project board và custom field"
PROJECT_NUMBER=$(gh variable list --repo "$SLUG" --json name,value --jq '.[] | select(.name=="PROJECT_NUMBER") | .value' 2>/dev/null || true)
if [ -z "$PROJECT_NUMBER" ]; then
  miss "vars.PROJECT_NUMBER chưa đặt — chạy 04-project.sh"
else
  pass "vars.PROJECT_NUMBER = $PROJECT_NUMBER"
  FIELDS=$(gh project field-list "$PROJECT_NUMBER" --owner "$ORG" --format json 2>/dev/null || echo '{"fields":[]}')
  for field in "Status" "Priority" "Component" "Phase" "Story Points" "Expert Review Required" "Iteration"; do
    if echo "$FIELDS" | jq -e --arg n "$field" '.fields[] | select(.name == $n)' > /dev/null; then
      pass "field '$field'"
    elif [ "$field" = "Iteration" ]; then
      miss "field 'Iteration' — API không tạo được, phải thêm tay ở Project settings"
    else
      miss "field '$field'"
    fi
  done

  STATUS_OPTS=$(echo "$FIELDS" | jq -r '.fields[] | select(.name=="Status") | .options[]?.name' | tr '\n' '|')
  for column in Backlog Todo "In Progress" "In Review" Testing Done; do
    echo "$STATUS_OPTS" | grep -q "$column" && pass "cột '$column'" || miss "cột '$column'"
  done
fi

if gh secret list --repo "$SLUG" --json name --jq '.[].name' 2>/dev/null | grep -qx "PROJECT_TOKEN"; then
  pass "secrets.PROJECT_TOKEN đã đặt"
else
  miss "secrets.PROJECT_TOKEN chưa đặt — project-automation sẽ hỏng ở bước kiểm tra cấu hình"
fi

# ── §8.4 — Ruleset ─────────────────────────────────────────────────────────────
step "§8.4 · Branch protection trên $DEFAULT_BRANCH"
RULES=$(api "repos/$SLUG/rules/branches/$DEFAULT_BRANCH" 2>/dev/null || echo '[]')
TYPES=$(echo "$RULES" | jq -r '[.[].type] | unique | join(" ")')

declare -A RULE_MEANING=(
  [deletion]="cấm xoá nhánh"
  [non_fast_forward]="cấm force push"
  [required_linear_history]="linear history"
  [pull_request]="bắt buộc PR"
  [required_status_checks]="bắt buộc status check"
  [required_signatures]="bắt buộc ký commit"
)
for rule in "${!RULE_MEANING[@]}"; do
  if echo "$TYPES" | grep -q "\b$rule\b"; then
    pass "${RULE_MEANING[$rule]} ($rule)"
  elif [ "$rule" = "required_signatures" ] && [ "$REQUIRE_SIGNED_COMMITS" != "true" ]; then
    info "bỏ qua ký commit (REQUIRE_SIGNED_COMMITS=false trong config.env)"
  else
    miss "${RULE_MEANING[$rule]} ($rule) chưa có hiệu lực"
  fi
done

PR_PARAMS=$(echo "$RULES" | jq -r '.[] | select(.type=="pull_request") | .parameters')
if [ -n "$PR_PARAMS" ]; then
  expect "số approval tối thiểu" "$(echo "$PR_PARAMS" | jq -r .required_approving_review_count)" "$REQUIRED_APPROVALS"
  expect "cần code owner duyệt"  "$(echo "$PR_PARAMS" | jq -r .require_code_owner_review)" "true"
  expect "phải giải quyết hết comment" "$(echo "$PR_PARAMS" | jq -r .required_review_thread_resolution)" "true"
  expect "huỷ approval cũ khi có push mới" "$(echo "$PR_PARAMS" | jq -r .dismiss_stale_reviews_on_push)" "true"
fi

SC_PARAMS=$(echo "$RULES" | jq -r '.[] | select(.type=="required_status_checks") | .parameters')
if [ -n "$SC_PARAMS" ]; then
  ACTUAL=$(echo "$SC_PARAMS" | jq -r '.required_status_checks[].context' | sort | tr '\n' ' ')
  for check in "${REQUIRED_CHECKS[@]}"; do
    echo "$ACTUAL" | grep -qw "$check" && pass "required check '$check'" || miss "required check '$check' chưa khai báo"
  done
  expect "bắt nhánh cập nhật với $DEFAULT_BRANCH" "$(echo "$SC_PARAMS" | jq -r .strict_required_status_checks_policy)" "true"
fi

BYPASS=$(api "repos/$SLUG/rulesets" --jq '[.[] | select(.target=="branch")] | .[0].id' 2>/dev/null || true)
if [ -n "$BYPASS" ] && [ "$BYPASS" != "null" ]; then
  N=$(api "repos/$SLUG/rulesets/$BYPASS" --jq '.bypass_actors | length')
  if [ "$N" = "0" ]; then
    pass "không có ai được bỏ qua ruleset"
  else
    warn "có $N actor được bỏ qua ruleset — chấp nhận được trong giai đoạn dựng nền"
    warn "đặt BOOTSTRAP_ADMIN_BYPASS=false trong config.env ngay khi org có người thứ hai"
  fi
fi

# ── Cách merge ─────────────────────────────────────────────────────────────────
step "§8.3 ⑥ · Cách merge"
R=$(api "repos/$SLUG")
expect "visibility"              "$(echo "$R" | jq -r .visibility)" "$VISIBILITY"
expect "cho phép squash merge"   "$(echo "$R" | jq -r .allow_squash_merge)" "true"
expect "cấm merge commit"        "$(echo "$R" | jq -r .allow_merge_commit)" "false"
expect "cấm rebase merge"        "$(echo "$R" | jq -r .allow_rebase_merge)" "false"
expect "tự xoá nhánh sau merge"  "$(echo "$R" | jq -r .delete_branch_on_merge)" "true"
expect "tiêu đề commit lấy từ PR" "$(echo "$R" | jq -r .squash_merge_commit_title)" "PR_TITLE"

# ── §8.5 — OIDC và môi trường ──────────────────────────────────────────────────
step "§8.5 · Môi trường và xác thực AWS"
for env in "${ENVIRONMENTS[@]}"; do
  if exists "repos/$SLUG/environments/$env"; then
    pass "environment '$env'"
  else
    miss "environment '$env'"
    continue
  fi
done

PROD=$(api "repos/$SLUG/environments/production" 2>/dev/null || echo '{}')
PROD_REVIEWERS=$(echo "$PROD" | jq '[.protection_rules[]? | select(.type=="required_reviewers") | .reviewers[]?] | length')
if [ "${PROD_REVIEWERS:-0}" -gt 0 ]; then
  pass "production có $PROD_REVIEWERS người/team duyệt bắt buộc"
else
  miss "production KHÔNG có required reviewers — cổng duyệt thủ công của §8.3 ⑦ không tồn tại"
fi

AWS_KEYS=$(gh secret list --repo "$SLUG" --json name --jq '.[].name' 2>/dev/null | grep -icE 'AWS_(ACCESS_KEY|SECRET)' || true)
if [ "${AWS_KEYS:-0}" -eq 0 ]; then
  pass "không có access key AWS trong Secrets (E1-07 AC 1)"
else
  miss "TÌM THẤY access key AWS trong Secrets — vi phạm trực tiếp §8.5"
fi

if grep -rq "AWS_ACCESS_KEY_ID" "$REPO_ROOT/.github/workflows/" 2>/dev/null; then
  miss "Có workflow tham chiếu AWS_ACCESS_KEY_ID"
else
  pass "không workflow nào tham chiếu access key tĩnh"
fi

if grep -q "id-token: write" "$REPO_ROOT/.github/workflows/cd-deploy.yml" 2>/dev/null; then
  pass "cd-deploy khai báo id-token: write (bắt buộc để OIDC hoạt động)"
else
  miss "cd-deploy thiếu 'id-token: write' — OIDC sẽ không lấy được token"
fi

# ── Bảo mật Actions ────────────────────────────────────────────────────────────
step "Bảo mật GitHub Actions"
WF=$(api "repos/$SLUG/actions/permissions/workflow" 2>/dev/null || echo '{}')
expect "quyền mặc định của GITHUB_TOKEN" "$(echo "$WF" | jq -r .default_workflow_permissions)" "read"
AP=$(api "repos/$SLUG/actions/permissions" 2>/dev/null || echo '{}')
expect "phạm vi action được phép" "$(echo "$AP" | jq -r .allowed_actions)" "selected"

SEC=$(echo "$R" | jq -r '.security_and_analysis.secret_scanning_push_protection.status // "unknown"')
expect "chặn đẩy bí mật lên repo" "$SEC" "enabled"

# ── Teams ──────────────────────────────────────────────────────────────────────
step "Team và quyền"
while read -r slug permission; do
  slug=$(echo "$slug" | base64 -d); permission=$(echo "$permission" | base64 -d)
  if exists "orgs/$ORG/teams/$slug"; then
    ACTUAL=$(api "orgs/$ORG/teams/$slug/repos/$SLUG" --jq '.role_name' 2>/dev/null || echo "không có quyền")
    if [ "$ACTUAL" = "$permission" ]; then
      pass "@$ORG/$slug ($permission)"
    else
      miss "@$ORG/$slug có quyền '$ACTUAL', mong đợi '$permission'"
    fi
  else
    miss "team @$ORG/$slug chưa tồn tại"
  fi
done < <(jq -r '.[] | [(.slug|@base64), (.permission|@base64)] | @tsv' "$SETUP_DIR/data/teams.json")

# ── Cổng eval ──────────────────────────────────────────────────────────────────
step "Cổng eval (docs/09 E7-05)"
EVAL_ENABLED=$(python_run "$REPO_ROOT/.github/scripts/gates.py" get enabled --file "$REPO_ROOT/eval/gates.yml" 2>/dev/null || echo "?")
if [ "$EVAL_ENABLED" = "true" ]; then
  pass "eval/gates.yml enabled: true — cổng đang chặn merge thật"
else
  warn "eval/gates.yml enabled: false — cổng eval CHƯA chặn merge."
  warn "Đây là trạng thái đúng cho tới khi E7-03 và E7-04 xong, nhưng chừng nào còn"
  warn "false thì tuyên bố 'không ai merge được thay đổi làm hệ thống trích dẫn văn"
  warn "bản hết hiệu lực' chưa thành hiện thực."
fi

# ── Kết luận ───────────────────────────────────────────────────────────────────
printf '\n%s────────────────────────────────────────%s\n' "$C_DIM" "$C_RESET"
if [ "$PROBLEMS" -eq 0 ]; then
  printf '%s✓ %d/%d mục kiểm tra đạt.%s\n' "$C_GREEN" "$CHECKS" "$CHECKS" "$C_RESET"
else
  printf '%s✗ %d/%d mục chưa đạt.%s\n' "$C_RED" "$PROBLEMS" "$CHECKS" "$C_RESET"
fi
exit $((PROBLEMS > 0 ? 1 : 0))
