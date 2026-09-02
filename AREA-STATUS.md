---
area_id: G-01/projects/rag-xnk
health: at-risk
last_activity: 2026-09-02
hours_last_7d: 0.00
key_metric: "L1–L4 của docs/19 xong: 6/9 mục services.json chạy được, 53 test xanh (31 .NET + 22 Python), smoke test 5/7 qua nginx. Sàn D-013 tuần 36 đạt 4/7 commit nội dung, hai ngày 31/08 và 01/09 vẫn là 0"
next_action: "L5 — Xnk.Chat gọi retrieval → generation, và generation nói chuyện LLM qua LLM_BASE_URL (Ollama, profile llm)"
blockers: []
updated_at: 2026-09-02
---

# Dự án RAG XNK

Mốc số 2 của chặng 1 trong `../../GOAL.md`. Kế hoạch thi công: [`docs/19`](docs/19-ke-hoach-skeleton-local.md).

**File này nằm trong git repo riêng của rag-xnk** (remote `sa-port-tech/RAG-XNK`), không phải
git của G-01 — vì `projects/rag-xnk/` bị `.gitignore` ở cấp mục tiêu. Đo nhịp độ phải chạy
`git -C projects/rag-xnk log`.

## Chạy được tới đâu

Một lệnh dựng cả hệ thống ở local:

```bash
cp .env.example .env && docker compose --profile app up -d
BASE_URL=http://localhost:8080 bash .github/scripts/smoke_test.sh
```

| Mục `services.json` | Trạng thái |
|---|---|
| `identity-tenant` (.NET) | ✅ phát JWT thật, PBKDF2, schema `identity` |
| `corpus` (.NET) | ✅ API tra cứu văn bản, cách ly tenant ở tầng SQL |
| `retrieval` (Python) | ✅ đọc `corpus` bằng SQL có điều kiện tenant, kiểm JWT |
| `ingestion` (Python) | ✅ readiness gọi API corpus; **không** chạm database (docs/00 §4.2) |
| `generation` (Python) | ✅ khung chạy; chưa có phụ thuộc nào cho tới khi có LLM |
| `shared` (thư viện) | ✅ |
| `chat` · `workflow-worker` · `web` | ❌ chưa dựng — L5, L6, L7 |

**Đã kiểm chứng từ database trống:** migration → vai trò → seed chạy tự động; đăng nhập bằng
tài khoản seed lấy được token; hai tenant gọi cùng một URL nhận hai kết quả khác nhau ở **cả
hai stack**; tắt PostgreSQL thì `/health/ready` trả 503 còn `/health/live` vẫn 200.

53 test xanh: 31 .NET (13 corpus + 18 identity-tenant) và 22 Python (15 retrieval + 4
ingestion + 3 generation).

## Ba ranh giới nay được thực thi, không chỉ ghi trong tài liệu

- **Vai trò database riêng cho từng service** (`db/roles.sql`, docs/00 §4.4). `xnk_retrieval`
  đọc được `corpus` nhưng `INSERT` bị PostgreSQL từ chối — đo được, không phải tin lời.
- **Cách ly tenant ở tầng SQL** (ADR-012), có test khoá lại *cơ chế* ở cả hai ngôn ngữ chứ
  không chỉ khoá kết quả.
- **Tiền tố đường dẫn của ALB** (ADR-013): nginx ở local không cắt tiền tố, nên hành vi đó
  được kiểm ở máy dev thay vì phát hiện sau khi deploy.

## Nhịp độ — chỗ chưa đạt

Sàn `D-013` là **≥1 commit nội dung mỗi NGÀY**. Tuần 36 tới hôm nay:

| Ngày | Commit nội dung |
|---|---|
| 31/08 | 0 |
| 01/09 | 0 |
| 02/09 | **4** |

4/7 của tuần, nhưng sàn là theo ngày và hai ngày đầu đã hụt — không lấy lại được bằng cách
dồn vào một hôm. Vì vậy `health` là `at-risk`, không phải `on-track`.

## Nhánh và remote

Đang ở `e1-11-skeleton-corpus-retrieval`, **chưa push**. Toàn bộ công việc vẫn nằm trên một
ổ đĩa: lệch 3 commit so với `origin/main` và cách nó hơn 90 file. Đó là L0 trong
[`docs/19`](docs/19-ke-hoach-skeleton-local.md) và là việc cần làm trước khi làm tiếp L5.
