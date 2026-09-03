# Thao tác GitHub — lệnh thật, id thật, cạm bẫy đã đo

Account ghi: **`Hoang-it`** (scope `repo` · `project` · `admin:org`). Account
`18520283-nhh` chỉ có `repo` + `read:org` — **chỉ review**, không ghi backlog. Đổi account:
`gh auth switch --user <tên>`.

Project #1 `RAG XNK — Prototype` · id `PVT_kwDOD9C04s4Bgew8`.

## 1. Sprint = milestone

```bash
# Tạo sprint. due_on là UTC — Chủ nhật 23:59 giờ VN = 16:59:59Z cùng ngày.
gh api repos/sa-port-tech/RAG-XNK/milestones -X POST \
  -f title='S36' -f state=open \
  -f due_on='2026-09-06T16:59:59Z' \
  -f description='Sprint Goal: <một câu>'

# Đọc tiến độ sprint — open/closed đọc thẳng từ API, cấm đếm tay
gh api repos/sa-port-tech/RAG-XNK/milestones --jq '.[] | "\(.title) open=\(.open_issues) closed=\(.closed_issues) due=\(.due_on)"'
```

## 2. Epic = issue cha · story = sub-issue

```bash
# Epic
gh issue create --title '[Epic] E2 — Corpus & Đồ thị hiệu lực' \
  --label 'type: epic' --label 'P0' --label 'component: ingestion' --body-file <file>

# Story, gắn milestone ngay khi tạo
gh issue create --title '[Story] E2-01 — Truy vấn hiệu lực theo thời điểm' \
  --label 'type: story' --label 'P0' --milestone 'S37' --body-file <file>

# Nối story vào epic. sub_issue_id là DATABASE id, KHÔNG phải số issue.
CHILD=$(gh api repos/sa-port-tech/RAG-XNK/issues/<số story> --jq .id)
gh api repos/sa-port-tech/RAG-XNK/issues/<số epic>/sub_issues -X POST -F sub_issue_id=$CHILD

# Kiểm lại
gh api repos/sa-port-tech/RAG-XNK/issues/<số epic>/sub_issues --jq '.[] | "\(.number) \(.title)"'
```

Nhờ sub-issue native, hai field `Parent issue` và `Sub-issues progress` trên board tự có
số — **cấm cộng tay % hoàn thành epic**.

## 3. Đưa lên board và đặt field

```bash
gh project item-add 1 --owner sa-port-tech --url <url issue>

# Lấy item-id của một issue trên board
gh project item-list 1 --owner sa-port-tech --format json \
  | python -c "import sys,json;[print(i['id'],i['content']['number']) for i in json.load(sys.stdin)['items']]"

gh project item-edit --project-id PVT_kwDOD9C04s4Bgew8 --id <item-id> \
  --field-id <field-id> --single-select-option-id <option-id>
```

| Field | field-id | Option-id |
|---|---|---|
| `Status` | `PVTSSF_lADOD9C04s4Bgew8zhfeN4E` | Backlog `2e83ea17` · Todo `8a099fce` · In Progress `11390c58` · In Review `3a6f01d3` · Testing `5fe3f165` · Done `37ce00cb` |
| `Priority` | `PVTSSF_lADOD9C04s4Bgew8zhfeOAY` | P0 `6a8a9106` · P1 `d8249523` · P2 `d8bd277f` · P3 `a5820c1b` |
| `Component` | `PVTSSF_lADOD9C04s4Bgew8zhfeOAc` | ingestion `e5f6b925` · retrieval `d0f1ba71` · generation `8e49b96f` · web `83338ff4` · infra `633cb5aa` · bpmn `7663a751` |
| `Phase` | `PVTSSF_lADOD9C04s4Bgew8zhfeODI` | Prototype `5f223fa4` · Phase 1 `56dd41ba` · Production `3e18058c` |
| `Expert Review Required` | `PVTSSF_lADOD9C04s4Bgew8zhfeOEA` | Có `3b9e8d64` · Không `4ac345ca` |
| `Story Points` (số) | `PVTF_lADOD9C04s4Bgew8zhfeODQ` | dùng `--number <n>` |
| `Iteration` | `PVTIF_lADOD9C04s4Bgew8zhfeQsM` | **chưa cấu hình chu kỳ nào** — xem §5 |

## 4. Comment bằng chứng khi chuyển cột

Mỗi lần đổi `Status`, để lại comment trên ticket theo khuôn ở `gates.md` §4:

```bash
gh issue comment <n> --body 'Todo → In Progress · nhánh `e1-11-skeleton-corpus-retrieval` · /devops'
```

Không có comment thì `docs/16` truy vết vào khoảng không.

## 5. Bốn cạm bẫy đã đo, đừng dẫm lại

1. **`GITHUB_TOKEN` không ghi được Projects v2** (`docs/17` §6.3). Mọi thao tác board đi
   qua `gh` với token `Hoang-it`, không qua workflow.
2. **Ruleset cấm push thẳng `main`**, kể cả của CI (`docs/17` §6.4). Commit của `scrum/`
   nằm trên nhánh đang làm việc.
3. **Field `Iteration` chưa có chu kỳ nào** — API không tạo được, phải bấm tay
   (`docs/17` §5.2). Tới khi bấm xong, **milestone là nguồn sự thật duy nhất** của sprint;
   cấm mọi vai báo cáo dựa trên `Iteration`.
4. **Board đang rỗng dù đã có 6 issue** — tự động hoá `project-automation.yml` hoặc chưa
   chạy, hoặc chạy hụt. Vai nào đưa ticket lên board thì **kiểm lại bằng `item-list`**,
   đừng tin là workflow đã làm hộ.

## 6. Nhãn còn thiếu

Kho nhãn có 28 mục nhưng **không có `type: epic`**. Tạo một lần:

```bash
gh label create 'type: epic' --color '5319e7' --description 'Nhóm story theo E1–E8 của docs/09'
```
