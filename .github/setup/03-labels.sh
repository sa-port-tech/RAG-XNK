#!/usr/bin/env bash
# Đồng bộ nhãn issue từ data/labels.json.
#
# Nhãn không phải chuyện thẩm mỹ: Issue Form gán nhãn tự động, và nhãn không tồn tại
# thì GitHub bỏ qua trong im lặng — issue mở ra sẽ thiếu phân loại mà không ai biết.
#
# Nhãn mặc định của GitHub (bug, enhancement, question…) bị xoá vì chúng chồng lấn
# với hệ nhãn có tiền tố ở đây và tạo ra hai cách gọi cùng một thứ.

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
require_gh
require_jq
require_python
confirm_repo

LABELS_FILE="$SETUP_DIR/data/labels.json"

step "Đồng bộ nhãn"
CREATED=0; UPDATED=0
while read -r name color description; do
  name=$(echo "$name" | base64 -d)
  color=$(echo "$color" | base64 -d)
  description=$(echo "$description" | base64 -d)

  encoded=$(jq -rn --arg n "$name" '$n|@uri')
  if exists "repos/$SLUG/labels/$encoded"; then
    gh api -X PATCH "repos/$SLUG/labels/$encoded" \
      -f new_name="$name" -f color="$color" -f description="$description" --silent
    UPDATED=$((UPDATED + 1))
  else
    gh api -X POST "repos/$SLUG/labels" \
      -f name="$name" -f color="$color" -f description="$description" --silent
    CREATED=$((CREATED + 1))
    ok "$name"
  fi
done < <(jq -r '.[] | [(.name|@base64), (.color|@base64), (.description|@base64)] | @tsv' "$LABELS_FILE")

info "Tạo mới: $CREATED · cập nhật: $UPDATED"

step "Xoá nhãn mặc định không dùng"
for legacy in "bug" "enhancement" "question" "help wanted" "invalid" "wontfix" "duplicate"; do
  encoded=$(jq -rn --arg n "$legacy" '$n|@uri')
  if exists "repos/$SLUG/labels/$encoded"; then
    gh api -X DELETE "repos/$SLUG/labels/$encoded" --silent
    ok "đã xoá '$legacy'"
  fi
done

step "Đối chiếu với nhãn mà Issue Form gán"
MISSING=0
while read -r label; do
  encoded=$(jq -rn --arg n "$label" '$n|@uri')
  if ! exists "repos/$SLUG/labels/$encoded"; then
    problem "Issue Form gán nhãn '$label' nhưng nhãn này không tồn tại."
    MISSING=1
  fi
done < <(
  python_run - "$REPO_ROOT/.github/ISSUE_TEMPLATE" <<'PY'
import pathlib, sys, yaml
root = pathlib.Path(sys.argv[1])
seen = set()
for path in sorted(root.glob("*.yml")):
    if path.name == "config.yml":
        continue
    data = yaml.safe_load(path.read_text(encoding="utf-8")) or {}
    for label in data.get("labels", []):
        seen.add(label)
for label in sorted(seen):
    print(label)
PY
)
[ "$MISSING" -eq 0 ] && ok "Mọi nhãn do Issue Form gán đều tồn tại"

exit $PROBLEMS
