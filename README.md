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
| Docker Desktop | có `docker compose` v2+ | cấp ≥6GB RAM nếu bật thêm profile `workflow` |
| .NET SDK | **9.0.x** | chỉ cần khi chạy/`dotnet test` ngoài container |
| uv | ≥0.5 | quản lý Python 3.12 cho ba service FastAPI |
| Git Bash | — | các script `.sh` cần bash; trên Windows dùng Git Bash |

### Hai lệnh

```bash
cp .env.example .env
docker compose --profile app up -d
```

Lệnh thứ hai dựng PostgreSQL, chạy migration + vai trò + seed, build bốn service, và bật
nginx làm cổng vào ở `http://localhost:8080`.

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
| `localhost:5432` | PostgreSQL — user `postgres`, mật khẩu `xnk-local-dev` |
| `http://localhost:8090` | Camunda Cockpit (chỉ với profile `workflow`) |
| `http://localhost:16686` | Jaeger UI (chỉ với profile `tracing`) |

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
dotnet test src/dotnet/Xnk.sln          # .NET — tự khởi Postgres bằng Testcontainers
cd src/python && uv run pytest           # Python
```

Không cần dựng gì trước: `PostgresFixture` tự khởi container `pgvector/pgvector:pg16` —
đúng ảnh mà CI dùng — khi không có biến `XNK_TEST_CONNECTION`.

---

## Kiến trúc rút gọn

Bảy service, tách theo ranh giới nghiệp vụ ([`docs/00`](docs/00-ke-hoach-tong-the.md) §4.2).
Danh mục chính thức nằm ở [`.github/services.json`](.github/services.json) — **nguồn sự thật
duy nhất** cho path filter của CI, ma trận build của CD, và định tuyến nginx ở local (ADR-009).

| Service | Stack | Sở hữu dữ liệu |
|---|---|---|
| `identity-tenant` | .NET | schema `identity`, `tenant` |
| `chat` | .NET | schema `conversation` |
| `corpus` | .NET | schema `corpus`, `lookup` |
| `workflow-worker` | .NET | không sở hữu |
| `ingestion` | Python | ghi qua API của `corpus` |
| `retrieval` | Python | đọc `corpus`, đọc-ghi `vector` |
| `generation` | Python | không sở hữu |

Mỗi service nối database bằng **vai trò riêng** chỉ có quyền trên schema của mình
([`db/roles.sql`](db/roles.sql)). Ngoại lệ duy nhất là `retrieval` được đọc `corpus`, vì lọc
hiệu lực phải nằm cùng một câu SQL với vector search — ADR-012.

**Trạng thái dựng:** xem `AREA-STATUS.md`. Chưa phải service nào cũng có mã; kế hoạch hoàn
thiện nằm ở [`docs/19`](docs/19-ke-hoach-skeleton-local.md).

---

## Trước khi mở PR

Đọc [`CONTRIBUTING.md`](CONTRIBUTING.md) — nhất là mục ký commit, vì repo **bắt buộc ký** và
không cấu hình trước thì push bị từ chối với một thông báo khó hiểu.

Ba việc không được làm, lý do ở `CONTRIBUTING.md`: **Java Delegate** trong file `.bpmn` ·
**nới ngưỡng trong `eval/gates.yml`** để PR xanh · **thêm access key AWS** vào Secrets.
