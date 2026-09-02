---
area_id: G-01/projects/rag-xnk
health: at-risk
last_activity: 2026-09-02
hours_last_7d: 3.63
key_metric: "Skeleton local XONG: 9/9 mục services.json chạy được, smoke test 7/7, 81 test xanh (48 .NET + 33 Python). Sàn D-013 tuần 36 đạt 7/7 commit nội dung, hai ngày 31/08 và 01/09 vẫn là 0. Sau 19:18 còn ba commit nữa (docs 20 đường ống hỏi-đáp 22:02, style _camelCase 22:43, gỡ CodeQL + sửa cổng BR-11 23:08) — tổng 17 commit tác giả trong ngày, 3.63h đo được từ khoảng cách commit"
next_action: "L0 — rebase nhánh lên origin/main, push, mở PR có Closes #n. Toàn bộ công việc vẫn nằm trên một ổ đĩa"
blockers: []
updated_at: 2026-09-02
---

# Dự án RAG XNK

Mốc số 2 của chặng 1 trong `../../GOAL.md`. Kế hoạch và kết quả thi công:
[`docs/19`](docs/19-ke-hoach-skeleton-local.md).

**File này nằm trong git repo riêng của rag-xnk** (remote `sa-port-tech/RAG-XNK`), không phải
git của G-01 — vì `projects/rag-xnk/` bị `.gitignore` ở cấp mục tiêu. Đo nhịp độ phải chạy
`git -C projects/rag-xnk log`.

## Chạy được tới đâu

```bash
cp .env.example .env && docker compose --profile app up -d
BASE_URL=http://localhost:8080 bash .github/scripts/smoke_test.sh
```

**9/9 mục trong `services.json` đã có mã và chạy được.** Smoke test đạt cho toàn bộ 7 service.

| Mục | Trạng thái |
|---|---|
| `identity-tenant` | ✅ phát JWT thật, PBKDF2 600k vòng, schema `identity` |
| `corpus` | ✅ API tra cứu, cách ly tenant ở tầng SQL |
| `retrieval` | ✅ đọc `corpus` bằng SQL có điều kiện tenant, tự kiểm JWT |
| `ingestion` | ✅ readiness gọi API corpus; **không** chạm database |
| `generation` | ✅ gọi LLM qua giao thức tương thích OpenAI, local là Ollama |
| `chat` | ✅ `POST /chat/ask` — gọi retrieval rồi generation, chuyển tiếp token người dùng |
| `workflow-worker` | ✅ External Task Worker, quy trình `P1` deploy sẵn trong image Camunda |
| `web` | ✅ Blazor WASM: đăng nhập, danh sách văn bản, gói 2120/3500 KB Brotli |
| `shared` | ✅ |

## Đã kiểm chứng từ database trống

| Kiểm | Kết quả |
|---|---|
| Smoke test qua nginx | **7/7 service** HTTP 200 |
| Hai tenant, cùng một URL | Hai danh sách khác nhau ở **cả** `corpus` (.NET) lẫn `retrieval` (Python) |
| Giao diện | Đăng nhập hai tài khoản khác tenant → hai danh sách khác nhau (kiểm trên trình duyệt) |
| Hỏi–đáp | `/chat/ask` trả lời từ mô hình local, kèm 9 căn cứ gồm cả SOP riêng của tenant |
| Quy trình BPMN | Instance khởi → worker nhận External Task, hoàn thành → dừng ở User Task đúng người |
| **Kiểm âm** | Tắt PostgreSQL → `/health/ready` **503**, `/health/live` vẫn **200** ở cả hai stack |
| Test | **81 xanh** — 48 .NET (9 chat + 13 corpus + 18 identity + 8 worker), 33 Python |

## Bốn ranh giới nay được thực thi, không chỉ ghi trong tài liệu

- **Vai trò database riêng cho từng service** (`db/roles.sql`, docs/00 §4.4). `xnk_retrieval`
  đọc được `corpus` nhưng `INSERT` bị PostgreSQL từ chối — đo được, không phải tin lời.
- **Cách ly tenant ở tầng SQL** (ADR-012), có test khoá lại *cơ chế* ở cả hai ngôn ngữ.
- **Danh tính người dùng đi hết chuỗi service**: `chat` chuyển tiếp token xuống `retrieval`
  và `generation`, có test riêng — mất mắt xích này thì lớp cách ly mất tác dụng ở chặng hai.
- **Tiền tố đường dẫn của ALB** (ADR-013): nginx local không cắt tiền tố, nên hành vi đó được
  kiểm ở máy dev thay vì phát hiện sau khi deploy.

## Phần còn thiếu là nghiệp vụ, không phải khung

Chưa có chunk, chưa có embedding, chưa có đồ thị hiệu lực, chưa có guardrail — đó là phạm vi
`E2`–`E4`. `docs/16` ghi BR-11 (cách ly tenant) đã có cài đặt và test; mười quy tắc còn lại
vẫn ở mức "có story".

## Nhịp độ — chỗ chưa đạt

Sàn `D-013` là **≥1 commit nội dung mỗi NGÀY**:

| Ngày | Commit nội dung |
|---|---|
| 31/08 | 0 |
| 01/09 | 0 |
| 02/09 | **7** |

Đủ 7 commit cho cả tuần, nhưng sàn tính theo **ngày**: 31/08 và 01/09 đã hụt, và dồn bảy
commit vào một hôm không lấp được hai ngày trống. Vì vậy `health` là `at-risk`, không phải
`on-track`.

## Nhánh và remote

Đang ở `e1-11-skeleton-corpus-retrieval`, **chưa push**. Đây là **L0** trong `docs/19` và là
việc duy nhất còn lại của kế hoạch đó: rebase lên `origin/main`, push, mở PR có `Closes #n`.
Tới khi làm xong, toàn bộ công việc vẫn nằm trên một ổ đĩa.
