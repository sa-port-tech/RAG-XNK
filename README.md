# RAG XNK

Hệ thống hỏi–đáp nghiệp vụ xuất nhập khẩu, trả lời kèm **trích dẫn chính xác tới Điều/Khoản**
và **không bao giờ dẫn chiếu tới điều khoản đã hết hiệu lực**.

Phạm vi hiện tại: prototype 7 tuần trên AWS Free Tier — xem [`docs/00`](docs/00-ke-hoach-tong-the.md)
để biết bối cảnh, [`docs/01`](docs/01-ke-hoach-prototype-scrum.md) để biết cách làm việc.

---

## Chạy ở máy mình

### Cần có

| | Bản | Ghi chú |
|---|---|---|
| Docker Desktop | có `docker compose` v2+ | cấp **≥8GB RAM**; lần dựng đầu tải khoảng **6GB** ảnh |
| .NET SDK | **9.0.x** | chỉ cần khi chạy/`dotnet test` ngoài container |
| uv | ≥0.5 | quản lý Python 3.12 cho ba service FastAPI |
| Git Bash | — | các script `.sh` cần bash; trên Windows dùng Git Bash |

### Ba lệnh

```bash
cp .env.example .env
bash tools/local/sinh-khoa.sh
docker compose --profile app up -d
```

Lệnh thứ hai sinh **khoá ký JWT của riêng máy bạn**. Nó bắt buộc, không phải tuỳ chọn:
giá trị mẫu trong `.env.example` cố tình ngắn hơn ngưỡng 32 byte nên service sẽ từ chối
khởi động nếu bỏ qua bước này. Chỗ đó từng có một khoá dùng được kèm lời dặn hãy thay —
và trên máy dev thì lời dặn đó không bao giờ được làm, nên nay bước thay là bắt buộc chứ
không phải là lời dặn.

Lệnh thứ ba dựng PostgreSQL, chạy migration + vai trò + seed, build **7 service + giao
diện**, kéo mô hình ngôn ngữ về, khởi Camunda, và bật nginx làm cổng vào ở
`http://localhost:8080`. Lần đầu mất khoảng 10–15 phút; những lần sau vài chục giây.

Cổng 8080 bận thì đặt `XNK_HTTP_PORT` trong `.env`.

**Đăng nhập bằng tài khoản mẫu** (mật khẩu `matkhau-local-2026`, xem `db/seed/`):

| Email | Tổ chức | Vai trò |
|---|---|---|
| `an.nguyen@noibo.vn` | Phòng XNK — nội bộ | admin |
| `chi.le@cangxanh.vn` | Công ty Giao nhận Cảng Xanh | admin |

Đăng nhập hai tài khoản này rồi so hai danh sách văn bản: phần "Dùng chung" giống nhau,
phần "Riêng" khác nhau. Đó là lớp cách ly tenant đang chạy, không phải một câu trong tài liệu.

Kiểm tra mọi thứ đã lên:

```bash
BASE_URL=http://localhost:8080 bash .github/scripts/smoke_test.sh
```

> Script lặp qua **toàn bộ** service khai trong `.github/services.json`. Service nào chưa
> được dựng sẽ báo lỗi — đó là sự thật cần thấy, không phải trục trặc cần lờ đi.

### Cổng

| Địa chỉ | Là gì |
|---|---|
| `http://localhost:8080` | Cổng vào duy nhất — nginx đóng vai ALB |
| `http://localhost:8080/corpus/health/ready` | Healthcheck của một service (đổi tên service theo `services.json`) |
| `http://localhost:8080/corpus/openapi/v1.json` | Hợp đồng API sinh từ mã (ADR-011) |
| `http://localhost:8080/chat/ask` | Hỏi–đáp: `POST {"question":"…"}` kèm `Authorization: Bearer` |
| `localhost:5432` | PostgreSQL — user `postgres`, mật khẩu `xnk-local-dev` |
| `http://localhost:8090` | Camunda Cockpit — user/mật khẩu mặc định `demo`/`demo` |
| `http://localhost:16686` | Jaeger UI (chỉ với profile `tracing`) |

**Thử một câu hỏi:**

```bash
TOKEN=$(curl -s localhost:8080/identity-tenant/token -H 'Content-Type: application/json' -d '{"email":"an.nguyen@noibo.vn","password":"matkhau-local-2026"}' | jq -r .access_token)

curl -s localhost:8080/chat/ask -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d '{"question":"Thủ tục hải quan với hàng nhập khẩu quy định ở văn bản nào?"}' | jq
```

Câu trả lời do mô hình **0.5B chạy trên CPU** sinh ra, mất 20–60 giây. Nó đủ để chứng minh
đường ống chạy, và **không** đủ để đánh giá chất lượng — đo chất lượng là việc của golden
set ([`docs/14`](docs/14-phuong-phap-golden-set.md)) trên mô hình thật.

**Thử quy trình BPMN:**

```bash
curl -s -X POST localhost:8090/engine-rest/process-definition/key/P1-dua-van-ban-vao-corpus/start -H 'Content-Type: application/json' -d '{"variables":{"van_ban_id":{"value":"39/2018/TT-BTC","type":"String"},"nguoi_duyet":{"value":"chi.le@cangxanh.vn","type":"String"}}}'

docker compose --profile app logs workflow-worker | tail -5
```

Worker nhận External Task, ghi thông báo, hoàn thành nó, và quy trình dừng ở User Task chờ
người duyệt.

**Đường dẫn luôn mang tiền tố tên service.** `http://localhost:8080/corpus/health/ready`
chứ không phải `/health/ready` — ALB không cắt tiền tố, và nginx ở đây tái hiện đúng hành
vi đó (ADR-013). Gọi không có tiền tố sẽ ra 404, và đó là chủ đích.

### Xem log, dừng, dựng lại

```bash
docker compose --profile app logs -f corpus     # log một service
docker compose --profile app ps                 # trạng thái
docker compose --profile app down               # dừng, giữ dữ liệu
docker compose --profile app down -v            # dừng và XOÁ database
docker compose --profile app up -d --build      # build lại sau khi sửa mã
```

### Chạy test

```bash
dotnet test src/dotnet/Xnk.sln                 # .NET
cd src/python && uv run pytest retrieval        # Python — MỘT service mỗi lần
```

Không cần dựng gì trước: cả hai phía tự khởi container `pgvector/pgvector:pg16` — đúng ảnh
mà CI dùng — khi không có biến `XNK_TEST_DATABASE_URL` trỏ sẵn. Một tên biến cho cả hai
bộ test; phía .NET tự quy đổi DSN sang chuỗi Npgsql.

**Python phải chạy từng service một.** Ba service có cùng tên gói `tests`, nên
`uv run pytest` gộp cả ba sẽ dừng với "import file mismatch". CI cũng chạy từng service một
qua matrix, nên đây là cách dùng đúng chứ không phải cách đi vòng.

Nếu `dotnet test` trên máy Windows dừng với `An Application Control policy has blocked this
file (0x800711C7)`, đó là chính sách bảo mật của máy chặn assembly vừa biên dịch — chạy bộ
test trong container thay vì đổi chính sách:

```bash
bash tools/local/chay-test.sh                   # cả solution
bash tools/local/chay-test.sh "Category=TenantIsolation"
```

---

## Kiến trúc rút gọn

Bảy service và một giao diện, tách theo ranh giới nghiệp vụ ([`docs/00`](docs/00-ke-hoach-tong-the.md) §4.2).
Danh mục chính thức nằm ở [`.github/services.json`](.github/services.json) — **nguồn sự thật
duy nhất** cho path filter của CI, ma trận build của CD, và định tuyến nginx ở local (ADR-009).

| Service | Stack | Sở hữu dữ liệu |
|---|---|---|
| `identity-tenant` | .NET | schema `identity`, `tenant` |
| `chat` | .NET | schema `conversation` *(chưa dựng — lát cắt hiện tại không lưu hội thoại)* |
| `corpus` | .NET | schema `corpus`, `lookup` |
| `workflow-worker` | .NET | không sở hữu |
| `ingestion` | Python | ghi qua API của `corpus` |
| `retrieval` | Python | đọc `corpus`; `vector` thuộc epic E3 (ADR-016) |
| `generation` | Python | không sở hữu |

Mỗi service nối database bằng **vai trò riêng** chỉ có quyền trên schema của mình
([`db/roles.sql`](db/roles.sql)). Ngoại lệ duy nhất là `retrieval` được đọc `corpus`, vì lọc
hiệu lực phải nằm cùng một câu SQL với vector search — ADR-012.

**Trạng thái dựng:** cả **9/9** mục trong `services.json` đã có mã và chạy được ở local.
Phần còn thiếu là **nghiệp vụ**, không phải khung: chưa có chunk, chưa có embedding, chưa
có đồ thị hiệu lực. Xem `AREA-STATUS.md` và [`docs/19`](docs/19-ke-hoach-skeleton-local.md).

## Bắt đầu một user story ở đâu

| Loại story | Chép mẫu nào |
|---|---|
| Endpoint .NET đọc/ghi dữ liệu | `Xnk.Corpus/Endpoints/DocumentsEndpoints.cs` — route → auth → tenant → EF → DTO → test |
| Endpoint Python chạm database | `retrieval/db.py` + `retrieval/main.py` — điều kiện tenant nằm trong câu SQL |
| Service .NET gọi service khác | `Xnk.Chat/Clients/` + `Http/ForwardAuthorizationHandler.cs` |
| Bước quy trình BPMN | `Xnk.WorkflowWorker/Handlers/` — thêm một `IExternalTaskHandler` |
| Màn hình giao diện | `Xnk.Web/Pages/DanhSachVanBan.razor` |

Riêng phần **chatbot AI** — nó nằm ở đâu, đang làm được gì và **chưa** làm được gì — đọc
[`docs/20`](docs/20-hien-trang-duong-ong-hoi-dap.md) trước khi đụng vào. Tóm tắt một dòng:
đường ống đã thông nhưng chữ "R" trong RAG chưa có, nên đừng đọc câu trả lời hiện tại rồi
kết luận gì về chất lượng hệ thống.

**Một điều đừng chép nhầm:** các endpoint **không** có dòng lọc tenant nào, và đó là chủ ý.
Bộ lọc nằm ở tầng SQL (ADR-012). Thêm một bộ lọc nữa ở tầng ứng dụng là tạo ấn tượng rằng
lọc là việc của endpoint — rồi endpoint tiếp theo sẽ quên.

---

## Trước khi mở PR

Đọc [`CONTRIBUTING.md`](CONTRIBUTING.md) — nhất là mục ký commit, vì repo **bắt buộc ký** và
không cấu hình trước thì push bị từ chối với một thông báo khó hiểu.

Ba việc không được làm, lý do ở `CONTRIBUTING.md`: **Java Delegate** trong file `.bpmn` ·
**nới ngưỡng trong `eval/gates.yml`** để PR xanh · **thêm access key AWS** vào Secrets.
