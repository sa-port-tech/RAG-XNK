---
area_id: G-01/projects/rag-xnk
health: at-risk
last_activity: 2026-09-10
hours_last_7d: 4.34
key_metric: "10/09: 10 commit 20:05-21:47 (~1.7h theo khoảng cách commit) — ten file migration sinh tự động, refactor 8 Dockerfile → 2 công thức tham số hoá, sửa CI python, registry văn bản corpus, sửa retry/health-check chat & worker, packages.lock.json thật. Khối kế hoạch 21:10-22:10 (60') KHÔNG tick trong file ngày — /checkout ghi theo bằng chứng git. hours_last_7d 4.34h ước theo cụm commit (07/09 1 commit lẻ không đo được · 09/09 hai cụm 08:04-08:35 + 21:56-23:57 ≈2.54h · 10/09 ≈1.7h) — PHƯƠNG PHÁP ước lượng, không phải hours7d.py"
next_action: "L0 — rebase nhánh e1-11-skeleton-corpus-retrieval lên origin/main, push (đang ahead nhiều commit), mở PR có Closes #n. Toàn bộ công việc vẫn nằm trên một ổ đĩa"
blockers:
  - "19/09: LẦN THỨ BA khối 'sàn ≥1 commit mã thật, đóng đợt 1/2 tuần' (19:55-20:55) TICK mà repo 0 commit — sau 11/09 và 12/09. Hai khối làm việc thật ngay trước (17:10-19:10, 2h kế hoạch) cũng không tick, cây làm việc sạch ngoài AREA-STATUS.md. Repo im lặng liên tục từ commit cuối 10/09/2026 — 9 ngày không một dòng code, trong khi lịch tuần vẫn xếp đều 3h/ngày cho mảng này"
  - "11/09 NGƯỢC CHIỀU với 10/09: hai khối 09:50-10:50 và 10:50-11:50 (2.00h, đợt ngày nghỉ) TICK ĐỦ mà repo có 0 commit, cây làm việc sạch. Bằng chứng duy nhất là .pytest_cache 11:26-11:28 — có chạy test, chưa tới chỗ commit được. Khối 3/3 (13:20-14:20) không tick. Cùng repo này hôm 10/09 sinh 10 commit trong 1.7h; hours_actual của cả ba khối để TRỐNG theo I-010"
  - "10/09 làm việc 20:05-21:47 nhưng khối lịch 21:10-22:10 không được tick trong file ngày — nợ tick, không phải nợ làm"
  - "Nhánh ahead nhiều commit chưa push. Chưa rõ PR #11 hiện đóng bao nhiêu/195 thread — cần đối chiếu lại khi mở phiên trong repo"
  - "hours_last_7d ở trên là ước lượng bằng khoảng cách commit trong cụm liền nhau, KHÔNG phải hours7d.py (script đó đọc time.csv của DailyTracker, không đọc git log của repo con) — /review nên đối chiếu lại nếu dùng số này cho phép tính capacity"
updated_at: 2026-09-19
so_lieu:                      # KHỐI MÁY GHI — người và Claude không sửa tay
  nguon: "trang_thai.py"
  tinh_luc: "2026-09-19 00:25"
  gio_nap_7d: 0.0
  gio_that_7d: 0.0
  dao_phut_7d: 0
  no_dao_phut: 0.0
  dao_dat: true
  san_chi_tieu: null
  san_thuc: null
  san_dat: null
  commit_7d: 1
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
