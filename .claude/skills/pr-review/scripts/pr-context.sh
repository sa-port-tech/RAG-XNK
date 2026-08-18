#!/usr/bin/env bash
#
# Thu thập toàn bộ bối cảnh cần có TRƯỚC khi review một PR của sa-port-tech/RAG-XNK.
#
# Vì sao có script này: mỗi lượt review đều cần đúng chừng này thông tin — ai là tác
# giả, chạm đường dẫn nhạy cảm nào, đã có ai duyệt chưa, commit cuối do ai đẩy, CI
# xanh chưa. Gọi tay 6 lệnh `gh` rời rạc mỗi lần vừa chậm vừa dễ sót, và sót thì hậu
# quả là một approval bị GitHub vô hiệu hoá mà không ai hiểu vì sao.
#
# Script KHÔNG đăng gì lên GitHub. Nó chỉ đọc.
#
# Dùng: bash .claude/skills/pr-review/scripts/pr-context.sh <số PR>

set -euo pipefail

PR="${1:-}"
if [ -z "$PR" ]; then
  echo "Dùng: pr-context.sh <số PR>" >&2
  exit 2
fi

ORG="sa-port-tech"
REPO="RAG-XNK"
NWO="$ORG/$REPO"
REVIEWER="18520283-nhh"

# ── Danh tính ────────────────────────────────────────────────────────────────────
# Lấy token của account phụ theo tên, KHÔNG dựa vào account đang active. `gh auth
# switch` là trạng thái toàn cục, dễ bị đổi ở terminal khác hoặc quên đổi lại — và
# review đăng nhầm danh tính thì phải xoá thủ công trên web.
TOKEN="$(gh auth token --user "$REVIEWER" 2>/dev/null || true)"
if [ -z "$TOKEN" ]; then
  cat >&2 <<EOF
✗ gh CLI chưa có account $REVIEWER.
  Chạy: gh auth login --hostname github.com --git-protocol ssh --skip-ssh-key --web
  (đăng nhập trình duyệt bằng $REVIEWER, dùng cửa sổ ẩn danh cho chắc)
EOF
  exit 1
fi
export GH_TOKEN="$TOKEN"
unset GITHUB_TOKEN 2>/dev/null || true

WHOAMI="$(gh api user --jq .login)"
if [ "$WHOAMI" != "$REVIEWER" ]; then
  echo "✗ Token đang trỏ tới '$WHOAMI', không phải '$REVIEWER'. Dừng." >&2
  exit 1
fi

# ── Siêu dữ liệu PR ──────────────────────────────────────────────────────────────
read -r AUTHOR TITLE <<EOF
$(gh pr view "$PR" --repo "$NWO" --json author,title --jq '.author.login + " " + .title')
EOF

STATE=$(gh pr view "$PR" --repo "$NWO" --json state --jq .state)
DRAFT=$(gh pr view "$PR" --repo "$NWO" --json isDraft --jq .isDraft)
HEAD=$(gh pr view "$PR" --repo "$NWO" --json headRefOid --jq .headRefOid)
DECISION=$(gh pr view "$PR" --repo "$NWO" --json reviewDecision --jq '.reviewDecision // "CHƯA CÓ"')
URL=$(gh pr view "$PR" --repo "$NWO" --json url --jq .url)

echo "══ PR #$PR ═════════════════════════════════════════════════════════════════"
echo "Tiêu đề    : $TITLE"
echo "Tác giả    : $AUTHOR"
echo "Trạng thái : $STATE (draft=$DRAFT)  ·  reviewDecision=$DECISION"
echo "HEAD       : $HEAD"
echo "URL        : $URL"
echo "Reviewer   : $WHOAMI  ✓ token đúng danh tính"

# ── Chặn tự duyệt ────────────────────────────────────────────────────────────────
# GitHub không cho tác giả approve PR của mình, và .github/scripts/pr-governance.cjs
# cũng loại review của tác giả khi đếm. Phát hiện sớm ở đây rẻ hơn phát hiện sau khi
# đã soạn xong bài review.
if [ "$AUTHOR" = "$REVIEWER" ]; then
  echo
  echo "🚫 PR này do CHÍNH $REVIEWER mở — không thể tự duyệt."
  echo "   Mở lại PR bằng account tác giả (Hoang-it), hoặc nhờ người khác duyệt."
fi

# ── Issue liên kết (luật pr-governance #2) ───────────────────────────────────────
ISSUES=$(gh api graphql -f query='
  query($owner:String!,$repo:String!,$number:Int!){
    repository(owner:$owner,name:$repo){
      pullRequest(number:$number){
        closingIssuesReferences(first:20){ nodes{ number } }
      }
    }
  }' -F owner="$ORG" -F repo="$REPO" -F number="$PR" \
  --jq '[.data.repository.pullRequest.closingIssuesReferences.nodes[].number] | map("#" + tostring) | join(", ")')

echo
echo "── Cổng pr-governance ──────────────────────────────────────────────────────"
if printf '%s' "$TITLE" | grep -Eq '^(feat|fix|docs|refactor|perf|test|build|ci|chore|revert)(\([a-z0-9][a-z0-9._/-]*\))?(!)?: .+'; then
  echo "Tiêu đề Conventional Commits : ✓"
else
  echo "Tiêu đề Conventional Commits : ✗  — squash merge lấy tiêu đề này làm dòng changelog"
fi
if [ -n "$ISSUES" ]; then
  echo "Issue liên kết               : ✓ $ISSUES"
else
  echo "Issue liên kết               : ✗  — thiếu 'Closes #<n>', issue sẽ không tự chuyển cột"
fi

# ── File thay đổi và đường dẫn nhạy cảm ──────────────────────────────────────────
FILES=$(gh api --paginate "repos/$NWO/pulls/$PR/files" --jq '.[].filename')
COUNT=$(printf '%s\n' "$FILES" | grep -c . || true)

echo
echo "── $COUNT file thay đổi ─────────────────────────────────────────────────────"
printf '%s\n' "$FILES" | sed 's/^/  /'

hits() { printf '%s\n' "$FILES" | grep -q "^$1" && return 0 || return 1; }

echo
echo "── Đường dẫn cần chú ý ─────────────────────────────────────────────────────"
ELEVATED=0
hits "infra/"                    && { echo "  ⚠ infra/                  → cần 2 approval (không tính tác giả)"; ELEVATED=1; }
hits "db/migrations/"            && { echo "  ⚠ db/migrations/          → cần 2 approval (không tính tác giả)"; ELEVATED=1; }
hits "eval/gates.yml"            && echo "  🔺 eval/gates.yml          → nguyên tắc bánh cóc: ngưỡng chỉ được đi lên"
hits ".github/quality-gates.yml" && echo "  🔺 .github/quality-gates.yml → như trên"
hits "eval/golden-set/"          && echo "  🔺 eval/golden-set/        → sửa thước đo; docs/14 gọi đây là bẫy số một"
hits "prompts/"                  && echo "  🔺 prompts/                → nội dung nghiệp vụ, cần chuyên gia XNK xác nhận"
hits "bpmn/"                     && echo "  🔺 bpmn/                   → cấm Java Delegate"
hits ".github/workflows/"        && echo "  🔺 .github/workflows/      → soi kỹ permissions và pull_request_target"
hits "src/dotnet/"               && echo "  · src/dotnet/             → warnings_as_errors=true, dotnet format phải sạch"
hits "src/python/"               && echo "  · src/python/             → ruff + mypy phải sạch"

# ── Review hiện có ───────────────────────────────────────────────────────────────
echo
echo "── Review hiện có ──────────────────────────────────────────────────────────"
REVIEWS=$(gh api --paginate "repos/$NWO/pulls/$PR/reviews" \
  --jq '.[] | select(.state != "COMMENTED" and .state != "PENDING") | .user.login + " " + .state')
if [ -n "$REVIEWS" ]; then
  printf '%s\n' "$REVIEWS" | sed 's/^/  /'
else
  echo "  (chưa có)"
fi

if [ "$ELEVATED" = "1" ]; then
  VALID=$(printf '%s\n' "$REVIEWS" | grep " APPROVED$" | grep -v "^$AUTHOR " | grep -c . || true)
  echo
  echo "  PR chạm đường dẫn nâng cao: đang có $VALID/2 approval hợp lệ."
  echo "  Chỉ có hai account thì tối đa đạt 1 — cần người thứ ba, hoặc tách PR."
fi

# ── Thread comment chưa giải quyết (ruleset đòi resolve hết) ─────────────────────
UNRESOLVED=$(gh api graphql -f query='
  query($owner:String!,$repo:String!,$number:Int!){
    repository(owner:$owner,name:$repo){
      pullRequest(number:$number){
        reviewThreads(first:100){ nodes{ isResolved } }
      }
    }
  }' -F owner="$ORG" -F repo="$REPO" -F number="$PR" \
  --jq '[.data.repository.pullRequest.reviewThreads.nodes[] | select(.isResolved == false)] | length')
echo
echo "Thread chưa resolve : $UNRESOLVED  (ruleset đòi resolve hết mới merge được)"

# ── Commit cuối — liên quan tới require_last_push_approval ───────────────────────
LAST_PUSHER=$(gh api "repos/$NWO/pulls/$PR/commits" --jq '.[-1].author.login // "?"')
echo "Người đẩy commit cuối : $LAST_PUSHER"
if [ "$LAST_PUSHER" = "$REVIEWER" ]; then
  echo "  🚫 require_last_push_approval: approval của $REVIEWER sẽ KHÔNG được tính,"
  echo "     vì chính account này đẩy commit cuối. Cần người khác duyệt."
fi

# ── CI ───────────────────────────────────────────────────────────────────────────
# ── CI ───────────────────────────────────────────────────────────────────────────
# `gh pr checks` thoát với mã khác 0 khi có check đỏ, nên không dùng được `||` để
# phát hiện "chưa có check". Lấy JSON rồi tự phân loại.
#
# Chỉ in cái chưa xanh. Repo này sinh ~25 check trên mỗi PR do các job dò đường dẫn,
# đổ hết ra màn hình thì phần quan trọng bị chôn ở giữa.
echo
echo "── Check trên HEAD ─────────────────────────────────────────────────────────"
CHECKS=$(gh pr checks "$PR" --repo "$NWO" --json name,bucket --jq '.[] | .bucket + "\t" + .name' 2>/dev/null || true)
if [ -z "$CHECKS" ]; then
  echo "  (chưa có check nào chạy)"
else
  NPASS=$(printf '%s\n' "$CHECKS" | grep -c '^pass' || true)
  NSKIP=$(printf '%s\n' "$CHECKS" | grep -c '^skipping' || true)
  echo "  pass=$NPASS · skipping=$NSKIP"
  BAD=$(printf '%s\n' "$CHECKS" | grep -Ev '^(pass|skipping)' || true)
  if [ -n "$BAD" ]; then
    echo "  Chưa xanh:"
    printf '%s\n' "$BAD" | sed 's/^/    /'
  fi
  # Bảy context bắt buộc trong ruleset — thiếu một cái là không merge được, kể cả
  # khi mọi check khác đều xanh.
  echo "  Context bắt buộc:"
  for ctx in ci-dotnet ci-python ci-blazor ci-bpmn eval-regression pr-governance codeql; do
    bucket=$(printf '%s\n' "$CHECKS" | awk -F'\t' -v c="$ctx" '$2==c {print $1; exit}')
    printf '    %-16s %s\n' "$ctx" "${bucket:-CHƯA CHẠY}"
  done
fi

echo
echo "════════════════════════════════════════════════════════════════════════════"
