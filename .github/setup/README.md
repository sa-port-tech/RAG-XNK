# Cấu hình GitHub dạng script

Thư mục này chứa **phần cấu hình GitHub không nằm được trong file repo**: ruleset,
Project board, environment, team, nhãn, chính sách Actions.

Vì sao viết thành script thay vì bấm tay theo hướng dẫn: cấu hình bấm tay không có
lịch sử, không review được, và không ai biết nó đã bị sửa khi nào. Với một dự án mà
cổng chất lượng là thứ giữ cho câu trả lời không sai luật, "ai đó đã tắt required
check hồi tháng trước" là một tình huống phải phòng chứ không phải phải chịu.

Mọi script đều **idempotent** — chạy lại lần thứ mười cho cùng kết quả như lần đầu.

---

## Dùng

```bash
bash .github/setup/apply-all.sh
```

Chạy lại một bước riêng lẻ:

```bash
bash .github/setup/apply-all.sh 06-ruleset.sh
```

Chỉ đối chiếu, không sửa gì:

```bash
bash .github/setup/99-verify.sh
```

---

## Điều kiện tiên quyết

| | |
|---|---|
| GitHub CLI | `winget install --id GitHub.cli` |
| Đăng nhập | `gh auth login` |
| Scope | `gh auth refresh -h github.com -s admin:org,project,workflow,repo` |
| Tổ chức | Đã tạo tay tại github.com/organizations/plan — **GitHub không có API tạo org** |
| File `.github/` | Đã có trên nhánh `main` trước khi chạy `06-ruleset.sh` |

Thứ tự cuối cùng quan trọng: đặt ruleset khi `main` còn trống là tự khoá mình ra
ngoài — từ lúc đó không push thẳng được nữa, mà cũng chưa có workflow nào để status
check báo cáo.

---

## Các script

| Script | Làm gì | Bảo vệ điều khoản nào |
|---|---|---|
| `00-preflight.sh` | Kiểm tra CLI, scope, org, quyền admin | — |
| `01-repo-settings.sh` | Visibility, chỉ squash merge, quét bí mật | docs/00 §8.3 ⑥, §8.4 |
| `02-teams.sh` | 9 team + quyền, đối chiếu với CODEOWNERS | docs/00 §8.6 |
| `03-labels.sh` | Nhãn, đối chiếu với nhãn Issue Form gán | docs/00 §8.2 |
| `04-project.sh` | Project v2, 6 cột, custom field | docs/00 §8.2 |
| `05-environments.sh` | dev/staging/production + người duyệt | docs/00 §8.5, §8.3 ⑦ |
| `06-ruleset.sh` | Bảo vệ nhánh `main` | docs/00 §8.4 |
| `07-actions-security.sh` | Siết quyền GITHUB_TOKEN, danh sách trắng action | docs/00 §8.5 |
| `99-verify.sh` | Đối chiếu hiện trạng với tài liệu | toàn bộ §8 |

---

## Ba việc script không làm được

GitHub không mở API cho ba thứ sau. `99-verify.sh` phát hiện và nhắc, nhưng phải bấm tay:

1. **Tạo Organization** — không có endpoint nào.
2. **Field kiểu Iteration trên Project** — enum `ProjectV2CustomFieldType` chỉ có
   TEXT, NUMBER, DATE, SINGLE_SELECT.
3. **Thêm thành viên vào team** — cần username GitHub thật, script không đoán được.
   Lệnh: `gh api -X PUT orgs/sa-port-tech/teams/<team>/memberships/<user> -f role=member`

---

## Sửa cấu hình

Tất cả tham số nằm trong [`config.env`](config.env) và [`data/`](data/). Không sửa
logic trong script để đổi một giá trị — nếu phải làm vậy nghĩa là giá trị đó đang bị
viết cứng sai chỗ, hãy đưa nó ra `config.env`.
