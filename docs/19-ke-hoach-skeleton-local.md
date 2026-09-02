# Kế hoạch hoàn thiện skeleton chạy local

> **Mục tiêu một câu:** một dev mới clone repo, chạy hai lệnh, và có toàn bộ hệ thống chạy
> trên máy mình — đủ để nhận một user story bất kỳ của Sprint 1–3 và bắt đầu gõ mã ngay,
> không phải dựng thêm gì.

| | |
|---|---|
| Trong phạm vi | Mọi thứ chạy được trên máy dev: 7 service + web, xác thực thật, dữ liệu thật, quy trình thật |
| **Ngoài phạm vi** | AWS, IaC, deploy ra internet, Telerik UI, tải văn bản pháp luật thật về `corpus/raw/`, bật cổng eval |
| Story liên quan | `E1-11` (hoàn thành nốt), và phần local của `E1-12` |

## Định nghĩa "xong" — bốn câu kiểm tra

Đợt này chỉ được đóng khi **cả bốn** câu dưới đây trả lời được bằng cách chạy lệnh, không
phải bằng cách đọc tài liệu:

1. `docker compose --profile app up -d` → cả 8 tiến trình lên, `bash .github/scripts/smoke_test.sh` với `BASE_URL=http://localhost:8080` **đạt cho toàn bộ 7 service**.
2. Lấy được token thật từ `identity-tenant` bằng tài khoản có trong database, dùng token đó gọi `corpus` và **thấy đúng phần dữ liệu của tenant mình** — hai tenant khác nhau cho hai kết quả khác nhau.
3. Mở `http://localhost:8080/` thấy giao diện Blazor thật, đăng nhập được, hiển thị danh sách văn bản lấy từ API.
4. `dotnet test` và `uv run pytest` xanh trên máy sạch, không cần biến môi trường nào ngoài `.env`.

**Không mock, không workaround** ở đây nghĩa cụ thể là ba điều: không có nhánh mã
`if (isLocal) return duLieuGia;` · không có endpoint trả dữ liệu cứng · không có service nào
báo `ready` khi phụ thuộc của nó chưa lên. Thiếu phụ thuộc thì readiness phải **đỏ**.

---

## 1. Hiện trạng — 5/9 mục của `services.json`

| Mục | Có gì | Thiếu gì |
|---|---|---|
| `corpus` (.NET) | Program, EF DbContext, migration, health, 4 test cách ly tenant | **chưa có một endpoint nghiệp vụ nào** |
| `ingestion` · `retrieval` · `generation` (Python) | `main.py`, health, 3 test hợp đồng đường dẫn | **chưa chạm database**, readiness đang trùng liveness |
| `shared` (.NET) | Tenancy, JWT validation, ServiceDefaults | — |
| `identity-tenant` · `chat` · `workflow-worker` · `web` | **không có thư mục** | tất cả |

Chỗ hổng nghiêm trọng nhất không phải bốn thư mục trống, mà là: `Xnk.Shared` biết **kiểm
tra** token, chưa ai **phát** token. Không có `identity-tenant` thì mọi story chạm dữ liệu
riêng của tenant đều không có đường bắt đầu.

---

## 2. Bảy quyết định kỹ thuật — chốt trước khi gõ dòng đầu

Ghi ra đây vì mỗi cái đều có phương án khác nghe hợp lý hơn, và chọn sai thì phải làm lại.

### D1 — Local phải có reverse proxy, không được gọi thẳng từng cổng

ADR-013 chốt ALB **không cắt tiền tố**, và mã hiện tại xử lý đúng điều đó
(`UsePathBase("/corpus")` bên .NET, `prefix="/retrieval"` bên Python). Nếu local mỗi service
một cổng và gọi thẳng, hành vi tiền tố **không bao giờ được kiểm chứng ở máy dev** — lỗi chỉ
lộ sau khi deploy. Thêm một container **nginx** nghe cổng 8080, định tuyến theo tiền tố,
**không strip**.

File `nginx.conf` **sinh từ `.github/services.json`** bằng `tools/local/render_nginx.py`, kèm
một test đối chiếu "sinh lại có khác file đang có không". Đây là hệ quả trực tiếp của ADR-009:
thêm service mà quên sửa proxy là loại lỗi phải chết ở CI, không phải ở người thứ hai.

### D2 — `identity-tenant` phát JWT HS256 bằng khoá đối xứng, và đó là giới hạn có hạn dùng

Local dùng đúng `XNK_JWT_SIGNING_KEY` trong `.env`, khớp với `JwtOptions` mà bảy service đang
xác thực. Mã thật: bảng `identity.users` + `identity.tenants`, mật khẩu băm bằng PBKDF2,
endpoint `POST /identity-tenant/token` trả JWT có ba claim `tenant_id`, `tenant_type`, `role`
theo đúng `TenantClaims`.

**Cái giá phải nói ra:** khoá đối xứng dùng chung nghĩa là **service nào cũng tự phát được
token hợp lệ**. Chấp nhận ở local; **cấm mang lên production**. Viết `ADR-014` chốt đường
chuyển sang khoá bất đối xứng + JWKS trước khi có môi trường thật.

### D3 — `generation` nói chuyện với LLM qua endpoint tương thích OpenAI

Không có Bedrock ở local, nhưng cũng **không được có nhánh mock**. Cách sạch: `generation`
đọc `LLM_BASE_URL` + `LLM_MODEL`, gọi giao thức `/v1/chat/completions`. Local trỏ vào
**Ollama** (profile `llm` trong compose, mô hình nhỏ). Cloud sau này thêm adapter Bedrock —
cùng interface, khác cài đặt.

Ollama chưa chạy → `/generation/health/ready` **trả 503**. Đó là hành vi đúng, không phải lỗi
cần vá. `ADR-015`.

### D4 — Python chạm database bằng SQL có bộ lọc tenant nằm trong câu truy vấn

`retrieval` đọc trực tiếp schema `corpus` (ADR-004). Dùng **SQLAlchemy 2.0 async + asyncpg**.
Điều kiện `WHERE tenant_id IS NULL OR tenant_id = :tenant` viết **trong câu SQL**, không lọc
sau khi lấy về — ADR-012 áp cho cả Python, không chỉ cho EF Core. Kèm một test chạy trên
Postgres thật chứng minh bộ lọc nằm trong SQL, đối xứng với
`Bo_loc_tenant_nam_trong_cau_SQL_chu_khong_o_tang_ung_dung` bên .NET.

### D5 — Migration local chạy đúng cơ chế mà cloud chạy

`cd-deploy` chạy migration bằng **ECS one-off task** trước khi deploy. Local dùng một service
compose one-shot gọi `db/apply-migrations.sh` — **không** dùng `dotnet ef database update`.
Lý do: `.sql` là hợp đồng liên ngôn ngữ (ADR-010), và ba service Python không nên phải cài SDK
.NET để có lược đồ.

### D6 — Dữ liệu seed là dữ liệu thật, không phải fixture bịa

Lấy từ `corpus/registry/van-ban.yaml` — 16 bản ghi có số hiệu và nguồn tra thật. Thêm hai
tenant thật (`noi_bo`, `b2b_khach_hang`) và vài văn bản riêng của tenant để bộ lọc **có gì để
lọc**. Đặt ở `db/seed/`, chạy được lặp lại.

⚠️ Chỉ seed **metadata**. Nội dung văn bản không nằm trong git (ADR-008), và trạng thái hiệu
lực vẫn để `null` — đó là thẩm quyền của chuyên gia, không phải của seed script.

### D7 — Blazor WASM thuần, không kéo Telerik

`docs/01` đặt Telerik ở Sprint 3 với trial 30 ngày. Kéo vào bây giờ là đốt trial trước khi cần,
và đẩy bundle về sát ngân sách 3500KB ngay từ ngày đầu. Skeleton dùng component chuẩn.

---

## 3. Chín lát cắt — thứ tự bắt buộc

Mỗi lát cắt kết thúc bằng **một commit thật** và một trạng thái **chạy được**. Không lát nào
để repo ở trạng thái nửa vời qua đêm. Cột "khối" tính theo khối 60'/ngày.

### L0 · Gỡ nghẽn — nhánh phải lên remote · **1 khối**

Không có bước này thì tám lát sau vẫn chỉ nằm trên một ổ đĩa.

- Tạo issue cho phần còn lại của `E1-11` (bắt buộc: `pr-governance` đòi `Closes #n`)
- Rebase nhánh lên `origin/main` — lấy 3 commit đang thiếu, xử lý phần trùng với `0f4d734`
- `git push -u origin` → mở PR → merge

**Xong khi:** `origin/main` chứa 4 service hiện có, CI xanh thật (không phải xanh vì skip).

### L1 · Một lệnh dựng cả hệ thống · **4 khối**

- `docker-compose.yml`: thêm profile `app` build từ 4 Dockerfile đang có, `depends_on` postgres healthy
- Service one-shot `migrate` chạy `db/apply-migrations.sh` (D5)
- `tools/local/render_nginx.py` sinh `nginx.conf` từ `services.json` (D1) + test chống lệch
- `db/seed/` với dữ liệu thật (D6)
- **`README.md`** — hiện đúng 11 byte. Viết quickstart thật: yêu cầu máy, hai lệnh, cách xem log, cách chạy test

**Xong khi:** máy sạch → `docker compose --profile app up -d` → smoke test đạt cho 4 service đang có.

### L2 · `identity-tenant` — service phát token · **5 khối**

- Project .NET, schema `identity`, migration EF + xuất `.sql` sang `db/migrations/`
- Bảng `tenants`, `users`; băm mật khẩu PBKDF2
- `POST /identity-tenant/token` trả JWT ba claim (D2)
- Test: token phát ra **được `Xnk.Shared` chấp nhận**; sai mật khẩu → 401; token hết hạn → 401
- `ADR-014` về giới hạn khoá đối xứng

**Xong khi:** `curl` lấy được token, dán vào `Authorization: Bearer` gọi `corpus` thấy 200.

### L3 · Lát cắt dọc .NET đầu tiên — mẫu cho mọi story sau · **3 khối**

- `GET /corpus/documents` có phân trang, `GET /corpus/documents/{id}`
- Không viết thêm dòng lọc tenant nào — global query filter đã lo (ADR-012). Đây chính là điều cần chứng minh.
- Test tích hợp qua HTTP với **hai token của hai tenant** → hai kết quả khác nhau

**Xong khi:** có một endpoint mẫu đầy đủ vòng đời để dev copy: route → auth → tenant → EF → DTO → test.

### L4 · Ba service Python chạm database thật · **4 khối**

- SQLAlchemy 2.0 async + asyncpg vào từng service (D4)
- `/health/ready` kiểm tra thật kết nối và sự tồn tại của schema — **gỡ dòng `TODO(E3)`** đang nói readiness trùng liveness
- `GET /retrieval/documents` đọc `corpus` với bộ lọc tenant trong SQL
- Test trên Postgres thật, đối xứng bộ test .NET

**Xong khi:** tắt Postgres → ba service Python báo `ready` đỏ, `live` vẫn xanh.

### L5 · `chat` — service gọi service · **4 khối**

- Project .NET, `POST /chat/ask`
- Gọi `retrieval` rồi `generation` bằng `HttpClient` có tên, timeout và retry rõ ràng
- `generation`: cài `LLM_BASE_URL` theo D3, profile `llm` với Ollama trong compose
- Truyền tiếp token xuống các service phía sau — **không** dùng token hệ thống ẩn danh

**Xong khi:** hỏi một câu qua `/chat/ask`, nhận câu trả lời sinh từ mô hình local, kèm danh sách văn bản mà `retrieval` đã trả.

### L6 · `workflow-worker` + BPMN thật · **3 khối**

- Project .NET đăng ký **External Task** với Camunda (Java Delegate bị cấm — `ci-bpmn` chặn)
- Một file `.bpmn` thật trong `bpmn/` (hiện chỉ có README) → `ci-bpmn` **hết skip**
- Compose: worker nằm trong profile `workflow`

**Xong khi:** khởi một process instance qua Cockpit ở `:8090`, worker nhận task và hoàn thành nó.

### L7 · `Xnk.Web` — Blazor WASM · **4 khối**

- Project Blazor WASM (D7), phục vụ qua nginx tại `/`
- Trang đăng nhập gọi `identity-tenant`, lưu token, gắn vào `HttpClient`
- Trang danh sách văn bản gọi `/corpus/documents`
- Xử lý lỗi theo `ProblemDetails` — `AddProblemDetails` đã bật sẵn ở mọi service
- `ci-blazor` **hết skip**, ngân sách bundle bắt đầu được đo thật

**Xong khi:** mở `http://localhost:8080/`, đăng nhập hai tài khoản khác tenant, thấy hai danh sách khác nhau.

### L8 · Chốt sổ · **2 khối**

- `CONTRIBUTING.md` thêm mục "Chạy toàn bộ hệ thống ở local"
- `docs/16` ma trận truy vết cập nhật cho `E1-11`
- `docs/README.md` chuyển ADR mới sang ✅
- Rà `services.json` ↔ đĩa: **9/9**

**Xong khi:** cả bốn câu kiểm tra ở đầu tài liệu này chạy đạt trên một máy sạch.

---

## 4. Tổng công sức và cái giá phải nói thẳng

| | Số |
|---|---|
| Tổng khối 60' | **30** |
| Ở nhịp một khối mỗi ngày | **4,3 tuần** |
| Sprint 0 theo `docs/01` | **1 tuần** |

Sprint 0 trên giấy dành 1 tuần cho việc mà một người làm hết 4,3 tuần. Con số đó không phải
lỗi của kế hoạch này — `docs/02` biên chế **7 người**, thực tế đang là **1**. Ai đọc tài liệu
Scrum rồi nhìn vào đây thì nên biết chênh lệch đó tới từ đâu, thay vì tưởng tiến độ chậm.

**Thứ tự ưu tiên nếu phải cắt:** L0 → L1 → L2 → L3 là **lõi không cắt được** (13 khối,
~2 tuần) — hết L3 là đã có đủ mẫu cho một dev .NET vào làm story. L4 mở đường cho dev Python.
L5–L7 mở đường cho story chat, quy trình và giao diện. L8 cắt được, đổi lại nợ tài liệu.

---

## 5. Sáu rủi ro đã lường

| # | Rủi ro | Xử |
|---|---|---|
| R1 | Khoá HS256 dùng chung → service nào cũng phát được token | Chỉ local. `ADR-014` chốt hạn chuyển sang JWKS **trước** môi trường thật |
| R2 | `nginx.conf` lệch `services.json` | Sinh tự động + test đối chiếu (D1) |
| R3 | `pytest` đặt `filterwarnings = ["error"]`, mypy `strict` | Thêm asyncpg/SQLAlchemy dễ làm đỏ CI vì một `DeprecationWarning`. Tính thời gian cho việc này trong L4 |
| R4 | Camunda + Ollama kéo thêm ~5GB ảnh | Giữ nguyên cơ chế profile: mặc định chỉ Postgres |
| R5 | Máy dev Windows | `apply-migrations.sh` cần Git Bash; `.gitattributes` đã xử CRLF — kiểm lại khi thêm script mới |
| R6 | Coverage đang đặt **0%** | Không nâng trong đợt này. Nâng lên 60 là việc cuối Sprint 1, theo ghi chú trong `quality-gates.yml` |

---

## 6. Việc KHÔNG làm trong đợt này

Ghi ra để không ai lặng lẽ kéo vào giữa chừng:

- **Bất kỳ dòng IaC nào** — CDK/Terraform thuộc `E1-04/05/06`, đợt sau
- **Bật `eval/gates.yml`** — cần bộ đo của `E7-03/E7-04`, chưa có
- **Tải văn bản pháp luật thật** về `corpus/raw/` — cần chuyên gia xác minh trước (`corpus/README.md` §4)
- **Telerik UI** — Sprint 3 (D7)
- **Nâng ngưỡng coverage** — cuối Sprint 1
- **Embedding và vector search thật** — đó là `E2`/`E3`, không phải skeleton
