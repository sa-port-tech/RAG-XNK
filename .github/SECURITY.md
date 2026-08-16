# Chính sách bảo mật

## Báo cáo lỗ hổng

**Không mở issue công khai cho lỗ hổng bảo mật.**

Dùng Private Vulnerability Reporting:
https://github.com/sa-port-tech/RAG-XNK/security/advisories/new

Chúng tôi phản hồi trong vòng **3 ngày làm việc**.

## Phạm vi

Repo này chứa mã nguồn và hạ tầng của prototype trợ lý hỏi–đáp pháp luật xuất nhập
khẩu. Những nhóm vấn đề sau được xem là lỗ hổng:

| Nhóm | Ví dụ |
|---|---|
| **Rò rỉ giữa tenant** | Truy vấn của tenant A trả về dữ liệu của tenant B. Đây là quy tắc BR-11 và là ranh giới bảo mật nghiêm ngặt nhất của hệ thống |
| Vượt quyền | Người dùng thường thao tác được chức năng quản trị |
| Prompt injection dẫn tới hành vi ngoài ý muốn | Nội dung trong văn bản được ingest điều khiển được câu trả lời cho người dùng khác |
| Lộ bí mật | Credential, khoá, token xuất hiện trong log, phản hồi API, hoặc trong lịch sử git |
| Chuỗi cung ứng | Workflow, action, hoặc phụ thuộc bị chèn mã |

## Không thuộc phạm vi bảo mật

**Câu trả lời sai về nghiệp vụ không phải lỗ hổng bảo mật** — kể cả khi hậu quả nghiêm
trọng. Trích dẫn văn bản hết hiệu lực, sai thuế suất, sai mã HS: mở issue bằng form
[📕 Corpus](https://github.com/sa-port-tech/RAG-XNK/issues/new?template=corpus-issue.yml)
hoặc [🐞 Bug](https://github.com/sa-port-tech/RAG-XNK/issues/new?template=bug.yml).

Hai luồng này tách nhau vì chúng cần người khác nhau xử lý và có nhịp khác nhau.

## Điều đã được xử lý sẵn trong repo

| | |
|---|---|
| Không có credential AWS dài hạn | OIDC federation — [`infra/oidc/`](../infra/oidc/README.md) |
| Chặn đẩy bí mật lên repo | Secret scanning push protection |
| `GITHUB_TOKEN` mặc định chỉ đọc | [`07-actions-security.sh`](setup/07-actions-security.sh) |
| Chỉ chạy action trong danh sách trắng | như trên |
| Quét mã tự động | [`codeql.yml`](workflows/codeql.yml) |
| Cấm push trực tiếp vào `main`, bắt buộc ký commit | Ruleset — [`06-ruleset.sh`](setup/06-ruleset.sh) |

## Nếu bí mật đã lộ

Xoá khỏi Secrets **chưa đủ** — bí mật đã tồn tại thì phải coi như đã bị lộ.
Xoay vòng khoá ở phía nhà cung cấp (IAM, Telerik, GitHub PAT) trước, rồi mới dọn repo.
