#!/usr/bin/env bash
# Tạo GitHub Projects v2 làm board và dựng đủ custom field (docs/00 §8.2).
#
# Board:  Backlog → Todo → In Progress → In Review → Testing → Done
# Field:  Priority · Component · Phase · Story Points · Iteration · Expert Review Required
#
# Một giới hạn thật của GitHub cần biết trước: kiểu field ITERATION không tạo được
# qua API. Enum ProjectV2CustomFieldType chỉ có TEXT, NUMBER, DATE, SINGLE_SELECT.
# Script sẽ tạo mọi thứ còn lại và nhắc rõ phần phải bấm tay.

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
require_gh
require_jq
confirm_repo

project_json() { gh project list --owner "$ORG" --limit 100 --format json; }

step "Tìm hoặc tạo Project"
NUMBER=$(project_json | jq -r --arg t "$PROJECT_TITLE" '.projects[] | select(.title == $t) | .number' | head -1)

if [ -n "$NUMBER" ]; then
  ok "Đã có Project #$NUMBER — \"$PROJECT_TITLE\""
else
  NUMBER=$(gh project create --owner "$ORG" --title "$PROJECT_TITLE" --format json | jq -r .number)
  ok "Đã tạo Project #$NUMBER"
fi

PROJECT_ID=$(gh project view "$NUMBER" --owner "$ORG" --format json | jq -r .id)
info "Project ID: $PROJECT_ID"

step "Liên kết repo với Project"
gh project link "$NUMBER" --owner "$ORG" --repo "$SLUG" > /dev/null 2>&1 \
  && ok "Đã liên kết $SLUG" \
  || info "Đã liên kết từ trước"

# ── Cột Status ────────────────────────────────────────────────────────────────
step "Đặt sáu cột cho field Status"
# GitHub tạo sẵn field Status với ba lựa chọn Todo/In Progress/Done. Phải ghi đè bằng
# GraphQL vì gh không có lệnh sửa lựa chọn của field có sẵn.
STATUS_FIELD_ID=$(gh project field-list "$NUMBER" --owner "$ORG" --format json \
  | jq -r '.fields[] | select(.name == "Status") | .id')

if [ -z "$STATUS_FIELD_ID" ] || [ "$STATUS_FIELD_ID" = "null" ]; then
  problem "Không tìm thấy field Status trên Project #$NUMBER."
else
  # Biến GraphQL kiểu mảng phải nằm trong body JSON. Cả `-f` lẫn `--raw-field` của
  # gh đều gửi giá trị dưới dạng CHUỖI, nên server trả lỗi "expected a key-value
  # object" dù nội dung là JSON hợp lệ. Cách đúng là POST thẳng {query, variables}.
  PAYLOAD=$(mktemp)
  jq -n --arg field "$STATUS_FIELD_ID" '{
    query: "mutation($field: ID!, $options: [ProjectV2SingleSelectFieldOptionInput!]!) { updateProjectV2Field(input: { fieldId: $field, singleSelectOptions: $options }) { projectV2Field { ... on ProjectV2SingleSelectField { id options { name } } } } }",
    variables: {
      field: $field,
      options: [
        {name: "Backlog",     color: "GRAY",   description: "Đã ghi nhận, chưa đưa vào sprint"},
        {name: "Todo",        color: "BLUE",   description: "Đã vào sprint, đạt Definition of Ready"},
        {name: "In Progress", color: "YELLOW", description: "Đã có nhánh, đang làm"},
        {name: "In Review",   color: "ORANGE", description: "PR đang mở, chờ review và CI"},
        {name: "Testing",     color: "PURPLE", description: "Đã merge, chờ deploy và PO nghiệm thu trên dev"},
        {name: "Done",        color: "GREEN",  description: "Đã deploy dev và được PO nghiệm thu"}
      ]
    }
  }' > "$PAYLOAD"

  RESULT=$(gh api graphql --input "$PAYLOAD" 2>&1) || {
    problem "Không đặt được cột Status: $RESULT"
    rm -f "$PAYLOAD"
  }
  rm -f "$PAYLOAD"

  if echo "$RESULT" | grep -q '"Backlog"'; then
    ok "Backlog → Todo → In Progress → In Review → Testing → Done"
  fi
fi

# ── Custom field ──────────────────────────────────────────────────────────────
create_field() {
  local name="$1" datatype="$2" options="${3:-}"

  if gh project field-list "$NUMBER" --owner "$ORG" --format json \
     | jq -e --arg n "$name" '.fields[] | select(.name == $n)' > /dev/null; then
    info "$name — đã có"
    return
  fi

  if [ -n "$options" ]; then
    gh project field-create "$NUMBER" --owner "$ORG" \
      --name "$name" --data-type "$datatype" --single-select-options "$options" > /dev/null
  else
    gh project field-create "$NUMBER" --owner "$ORG" \
      --name "$name" --data-type "$datatype" > /dev/null
  fi
  ok "$name ($datatype)"
}

step "Tạo custom field"
create_field "Priority"   SINGLE_SELECT "P0,P1,P2,P3"
create_field "Component"  SINGLE_SELECT "ingestion,retrieval,generation,web,infra,bpmn"
create_field "Phase"      SINGLE_SELECT "Prototype,Phase 1,Production"
create_field "Story Points" NUMBER
# docs/00 §8.2 gọi đây là trường boolean. Projects v2 không có kiểu boolean, nên dùng
# single-select hai lựa chọn — cùng tác dụng, và lọc trên board vẫn hoạt động.
create_field "Expert Review Required" SINGLE_SELECT "Có,Không"

step "Field Iteration"
if gh project field-list "$NUMBER" --owner "$ORG" --format json \
   | jq -e '.fields[] | select(.name == "Iteration")' > /dev/null; then
  ok "Đã có"
else
  warn "PHẢI TẠO BẰNG TAY — API của GitHub không tạo được field kiểu Iteration."
  info "Vào: https://github.com/orgs/$ORG/projects/$NUMBER/settings/fields"
  info "  → New field → Iteration → đặt tên \"Iteration\""
  info "  → độ dài 2 tuần (docs/01 §6), bắt đầu từ ngày khởi động Sprint 0"
fi

# ── Nối vào workflow ──────────────────────────────────────────────────────────
step "Đặt biến PROJECT_NUMBER cho repo"
gh variable set PROJECT_NUMBER --repo "$SLUG" --body "$NUMBER"
ok "vars.PROJECT_NUMBER = $NUMBER"

step "Nhắc việc còn lại cho con người"
info "Workflow project-automation cần secrets.PROJECT_TOKEN — một fine-grained PAT:"
info "  https://github.com/settings/personal-access-tokens/new"
info "  Resource owner: $ORG"
info "  Organization permissions → Projects: Read and write"
info "  Repository permissions   → Issues: Read · Pull requests: Read · Metadata: Read"
info "Rồi lưu vào repo:"
info "  gh secret set PROJECT_TOKEN --repo $SLUG"
info ""
info "Lý do không dùng GITHUB_TOKEN: nó không có quyền ghi Projects v2 cấp tổ chức,"
info "và không có khoá permissions nào cấp được quyền đó."

printf '\nProject: %shttps://github.com/orgs/%s/projects/%s%s\n' "$C_BLUE" "$ORG" "$NUMBER" "$C_RESET"
exit $PROBLEMS
