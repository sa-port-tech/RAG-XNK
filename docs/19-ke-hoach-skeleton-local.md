# Kế hoạch thi công — skeleton chạy local

> **Mục tiêu một câu:** một dev mới clone repo, chạy hai lệnh, có toàn bộ hệ thống chạy trên
> máy mình — đủ để nhận một user story Sprint 1–3 và bắt đầu gõ mã ngay, không phải dựng
> thêm gì.

| | |
|---|---|
| Trong phạm vi | Mọi thứ chạy được trên máy dev: 7 service + web, xác thực thật, dữ liệu thật, quy trình thật |
| **Ngoài phạm vi** | AWS, IaC, deploy ra internet, Telerik UI, tải văn bản pháp luật thật, bật cổng eval, schema `vector` |
| Story | `E1-11` (hoàn thành nốt) + phần local của `E1-12` |

## Trạng thái: L0–L8 xong, trừ L0 (02/09/2026)

| Lát | Trạng thái |
|---|---|
| L0 — đưa nhánh lên remote | ⬜ **chưa làm** — toàn bộ công việc vẫn nằm trên nhánh local |
| L1 — một lệnh dựng cả hệ thống | ✅ |
| L2 — `identity-tenant` | ✅ |
| L3 — lát cắt dọc `corpus` | ✅ |
| L4 — ba service Python | ✅ |
| L5 — `chat` + LLM | ✅ |
| L6 — `workflow-worker` + BPMN | ✅ |
| L7 — `Xnk.Web` | ✅ |
| L8 — chốt sổ | ✅ |

**Bốn câu kiểm tra ở mục 4 đều đạt trên database trống.** `services.json` khớp đĩa **9/9**.

### Bảy chỗ lệch khỏi kế hoạch, và lý do

| Chỗ | Vì sao |
|---|---|
| Bind mount → **COPY vào image** cho `migrate`, `nginx-conf`, `camunda`, `web` | Ổ ảo Google Drive không chia sẻ được vào WSL2, và Docker mount thành thư mục **rỗng mà không báo lỗi**. Đóng gói vào image cũng làm local giống ECS/S3 hơn |
| **Ollama và Camunda nằm trong profile `app`**, không phải profile riêng | `generation` và `workflow-worker` có phụ thuộc thật vào chúng. Để ngoài thì lệnh trong README cho ra một hệ thống có hai service báo lỗi — và như vậy không phải "hệ thống chạy được". Cái giá: ~6GB ảnh ở lần dựng đầu |
| Cổng host thành `${XNK_HTTP_PORT}` | 8080 là cổng bận nhất trên máy lập trình viên; không có lối này thì người bị trùng cổng phải sửa `docker-compose.yml` |
| Xuất SQL migration và chạy test **qua container** (`tools/local/`) | Application Control của Windows chặn `dotnet-ef` và assembly test vừa build (`0x800711C7`), không đều. Sửa chính sách bảo mật máy là cái giá sai |
| **D1 bỏ cổng CI canh `nginx.conf`** | Sinh lúc container khởi động thì lệch là bất khả thi — không cần thêm một cổng canh một file lẽ ra không nên tồn tại |
| **D8: `vector` để lại cho E3**, và thêm `ADR-016` | Gỡ mâu thuẫn mở giữa ADR-010 (EF sở hữu lược đồ) và ADR-012 (bảng `vector` thuộc `retrieval`, một service Python) |
| Thêm PostgreSQL vào `ci-python` | Test chạm DB của `retrieval` phải chạy trên CI, nếu không thì bộ lọc tenant phía Python không được canh ở đâu cả |

### Ba việc lộ ra khi thi công, không có trong kế hoạch

- `smoke_test.sh` báo sai "thiếu health_path" cho cả 7 service trên máy Windows: `jq` bản
  Windows xuất CRLF, `mapfile -t` để lại ký tự CR ở cuối mỗi tên. Lỗi **có sẵn**, chỉ không lộ trên CI Linux.
- Camunda 7.21 bắt buộc khai `historyTimeToLive`. Đó là một quyết định lưu trữ dữ liệu, nay
  ghi thẳng trong model kèm lý do chọn 30 ngày.
- `PhienDangNhap` đăng ký `Scoped` làm `IHttpClientFactory` dựng `TokenHandler` trong scope
  riêng, nhận về một thể hiện khác — đăng nhập báo thành công rồi mọi lời gọi API trả 401.

### Phần còn thiếu là NGHIỆP VỤ, không phải khung

Chưa có chunk, chưa có embedding, chưa có đồ thị hiệu lực, chưa có guardrail. Đó đúng là
phạm vi của `E2`–`E4` và **cố ý** nằm ngoài đợt này — xem mục 7.

---

## Bối cảnh

Phần khung quy trình của repo đã rất kỹ — 9 workflow, 7 required check, CODEOWNERS, ruleset —
nhưng phần **chạy được** mới có **5/9** mục khai trong `.github/services.json`. Bốn mục thiếu
(`identity-tenant`, `chat`, `workflow-worker`, `web`) không có thư mục.

Chỗ hổng nghiêm trọng nhất không phải bốn thư mục trống, mà là: `Xnk.Shared` biết **kiểm tra**
JWT nhưng **chưa service nào phát JWT**. Không có nó thì mọi user story chạm dữ liệu riêng của
tenant đều không có đường bắt đầu.

## Ba luật "không mock" — áp cho mọi lát cắt

1. Không có nhánh mã `if (isLocal) return duLieuGia;`
2. Không có endpoint trả dữ liệu cứng
3. **Không service nào báo `ready` khi phụ thuộc chưa lên** — thiếu Postgres hoặc LLM thì
   readiness phải trả **503**

---

## 1. Mười quyết định chốt

| # | Quyết định | Lý do |
|---|---|---|
| **D1** | nginx local nghe `:8080`, định tuyến theo tiền tố, **không strip**. `nginx.conf` **sinh lúc container khởi động** từ `.github/services.json` bởi một init container `python:3.12-slim`, ghi vào volume dùng chung — **không commit file sinh ra** | ADR-013: ALB không cắt tiền tố, nên local phải tái hiện đúng hành vi đó. Sinh lúc boot thì lệch với `services.json` là bất khả thi, không cần thêm cổng CI để canh |
| **D2** | `identity-tenant` phát JWT HS256 thật: bảng `identity.tenants` + `identity.users`, mật khẩu băm PBKDF2, `POST /identity-tenant/token` | Không có nó thì L3–L7 không có token để chạy |
| **D3** | `generation` gọi endpoint tương thích OpenAI qua `LLM_BASE_URL`. Local: **Ollama trong compose** (khi thi công đã chuyển vào profile `app` — xem bảng lệch ở đầu file) | Đường gọi thật ở cả hai phía; khác nhau ở cấu hình chứ không ở nhánh mã |
| **D4** | `retrieval` đọc `corpus` bằng SQLAlchemy 2.0 async + asyncpg, điều kiện tenant **nằm trong câu SQL** | ADR-012 áp cho cả Python, không chỉ cho EF Core |
| **D5** | Migration local chạy qua container one-shot gọi `db/apply-migrations.sh` | Đúng cơ chế ECS one-off task của `cd-deploy`; ADR-010. Không dùng `dotnet ef database update` |
| **D6** | Seed từ `corpus/registry/van-ban.yaml` (16 bản ghi thật) + 2 tenant thật. **Chỉ metadata**, `trang_thai` giữ `null` | ADR-008; thẩm quyền xác nhận hiệu lực thuộc chuyên gia, không thuộc seed script |
| **D7** | Blazor WASM thuần, **không** Telerik | Trial 30 ngày để dành Sprint 3; và bundle budget 3500KB |
| **D8** | Schema `vector` **ngoài phạm vi đợt này**; `retrieval` readiness chỉ kiểm `corpus`. `ADR-016` chốt: DDL của `vector` đi qua pipeline EF → `db/migrations/*.sql`, còn **quyền sở hữu dữ liệu** vẫn của `retrieval` | Gỡ mâu thuẫn mở giữa ADR-010 (EF sở hữu lược đồ) và ADR-012 (bảng `vector` thuộc `retrieval` — một service Python) |
| **D9** | **Mỗi service một DB role riêng** kèm GRANT, trong `db/roles.sql` (idempotent, **không** đặt trong `db/migrations/`) | `docs/00` §4.4. Local dùng chung `postgres` thì ranh giới sở hữu dữ liệu chỉ là câu văn — và lần đầu nó được thực thi thật sẽ là lúc lên AWS, tức chỗ đắt nhất để phát hiện mình đã JOIN chéo schema |
| **D10** | `ingestion` ghi **qua API của `corpus-service`**, không chạm DB. `generation` **không** chạm DB | `docs/00` §4.2 phân vai rõ: chỉ `retrieval` được đọc trực tiếp `corpus` |

---

## 2. Mẫu bắt buộc khi thêm một service .NET

Dùng cho L2, L5, L6. Chín bước, đúng thứ tự, tất cả đều tái sử dụng thứ đã có:

1. `dotnet new web` → `src/dotnet/Xnk.<Ten>/`, rồi `dotnet sln add` vào `src/dotnet/Xnk.sln`
2. `.csproj` dùng `Microsoft.NET.Sdk.Web`, khai `<RootNamespace>` + `<UserSecretsId>`, `ProjectReference` tới `../Xnk.Shared/Xnk.Shared.csproj`
3. **Thêm gói bằng CPM**: một dòng `<PackageVersion>` trong `Directory.Packages.props`, một dòng `<PackageReference>` **không Version** trong `.csproj`. `dotnet add package` **hỏng** khi bật CPM ở SDK 9
4. `Program.cs` gọi đúng bộ đã có: `AddXnkServiceDefaults()` · `AddXnkJwtAuthentication(builder.Configuration)` · `app.UsePathBase("/<ten trong services.json>")` **trước mọi middleware định tuyến** · `UseAuthentication`/`UseAuthorization` · `MapXnkHealthEndpoints()` · `MapOpenApi()`
5. Health check phụ thuộc gắn tag `ServiceDefaultsExtensions.ReadinessTag` — chỉ readiness, **không** đụng liveness
6. `appsettings.json` (giá trị rỗng) + `appsettings.Development.json` (giá trị local, khớp `.env.example`)
7. `Dockerfile` theo mẫu `Xnk.Corpus/Dockerfile`: **build context là gốc repo**, chép `.csproj` trước rồi `restore`, `USER app`, `EXPOSE 8080`, `ENTRYPOINT ["dotnet","Xnk.<Ten>.dll"]` (vì `UseAppHost=false`)
8. Project test `Xnk.<Ten>.Tests` theo mẫu `Xnk.Corpus.Tests.csproj`; dùng lại `PostgresFixture`/`PostgresCollection` nếu chạm DB
9. **Không sửa `.github/services.json`** — cả 9 mục đã khai sẵn, chỉ cần đường dẫn tồn tại

**Ba bẫy đã biết, đừng đạp lại:**

- `TreatWarningsAsErrors=true` + `AnalysisLevel=latest` → mã mới phải sạch cảnh báo ngay từ commit đầu
- `UseAppHost=false` tồn tại để repo build được trên Google Drive — **không được bật lại**
- `InvariantGlobalization=true` → đừng dựa vào sắp xếp chuỗi tiếng Việt

---

## 3. Chín lát cắt

Mỗi lát kết thúc bằng **một commit thật** và một trạng thái **chạy được**. Không lát nào để
repo ở trạng thái nửa vời qua đêm. Cột "khối" = số buổi 60'.

### L0 · Đưa nhánh lên remote · 1 khối

- Mở issue bằng form ✨ **Story / Feature** cho phần còn lại của `E1-11` — `pr-governance` bắt buộc PR có `Closes #n`
- Rebase `e1-11-skeleton-corpus-retrieval` lên `origin/main`: lấy `fc40ad3`, `0f4d734`, `82b63f6`; xử lý phần trùng vì `acd1440` + `20ecd17` đã được squash vào `0f4d734`
- `git push -u origin` → PR → merge

**Xong khi:** `origin/main` chứa 4 service hiện có; `ci-dotnet` và `ci-python` xanh **thật**, không phải xanh vì skip.

### L1 · Một lệnh dựng cả hệ thống · 5 khối

Đụng: `docker-compose.yml` · `db/roles.sql` (mới) · `db/seed/` (mới) · `tools/local/render_nginx.py` (mới) · `.env.example` · `README.md`

- **Profile `app`**: build 4 Dockerfile hiện có với `context: .`, `depends_on: postgres (service_healthy)`
- **`migrate` one-shot** (D5): image có `psql`, chạy `db/apply-migrations.sh` rồi `db/roles.sql`, `restart: "no"`
- **`db/roles.sql`** (D9): `xnk_identity`, `xnk_corpus`, `xnk_chat`, `xnk_retrieval`; mỗi role `GRANT USAGE` + CRUD trên schema của mình; `GRANT SELECT ON ALL TABLES IN SCHEMA corpus TO xnk_retrieval` — ngoại lệ ADR-012, ghi chú thẳng trong file; `ALTER DEFAULT PRIVILEGES` để bảng sinh sau cũng theo. Migration vẫn chạy bằng `postgres` (chủ sở hữu)
- **`nginx` + init `nginx-conf`** (D1): init render conf vào volume dùng chung; `proxy_pass http://<name>:<port>` **không có dấu `/` cuối** để giữ nguyên URI
- **Seed** (D6): 2 tenant + 16 văn bản dùng chung + vài văn bản riêng tenant, để bộ lọc có gì mà lọc
- **`README.md`** — hiện đúng 11 byte. Viết quickstart thật: yêu cầu máy, hai lệnh, bảng cổng, cách xem log, cách chạy test

**Xong khi:** máy sạch → `docker compose --profile app up -d` → `BASE_URL=http://localhost:8080 bash .github/scripts/smoke_test.sh` đạt cho 4 service đang có.

### L2 · `identity-tenant` — service phát token · 5 khối

Đụng: `src/dotnet/Xnk.IdentityTenant/**` · `Xnk.IdentityTenant.Tests/**` · `db/migrations/0002_*.sql` · `Directory.Packages.props` · `docs/adr/014-*.md`

- Theo đủ 9 bước mẫu ở §2
- `IdentityDbContext` sở hữu schema `identity`: `tenants` (id, tên, loại theo `TenantClaims.TenantType`), `users` (email, hash PBKDF2, salt, tenant_id, role)
- `POST /identity-tenant/token` nhận email + mật khẩu, trả JWT ký HS256 bằng đúng `Jwt:SigningKey`, ba claim **đúng tên hằng trong `TenantClaims`**
- Migration EF → **xuất `.sql` idempotent, commit cùng PR** (ADR-010). Lưu ý: `/db/migrations/` kéo thêm `data-lead` + `backend-lead` vào CODEOWNERS, và ruleset đòi **≥2 approval**
- Test: token phát ra **được `AddXnkJwtAuthentication` chấp nhận** · sai mật khẩu → 401 · token hết hạn → 401
- `ADR-014`: khoá đối xứng dùng chung nghĩa là **service nào cũng phát được token hợp lệ** — chỉ hợp lệ ở local, chốt đường chuyển sang JWKS trước môi trường thật

**Xong khi:** `curl` lấy được token, dùng nó gọi một endpoint cần auth của `corpus` → 200.

### L3 · Lát cắt dọc .NET đầu tiên — mẫu cho mọi story sau · 3 khối

Đụng: `Xnk.Corpus/Endpoints/` · `Xnk.Corpus/Contracts/` · `Xnk.Corpus.Tests/Api/`

- `GET /corpus/documents` (phân trang) + `GET /corpus/documents/{id}`, có `[Authorize]`
- **Không viết thêm một dòng lọc tenant nào** — global query filter đã lo. Đây chính là điều cần chứng minh, và là bài học quan trọng nhất mà dev sau phải nhìn thấy
- DTO riêng, không lộ entity EF ra ngoài
- Test tích hợp qua HTTP với **hai token của hai tenant** → hai kết quả khác nhau
- Giữ nguyên trait `Category=TenantIsolation` của nhóm test cũ — job `tenant-isolation` bật `TreatNoTestsAsError`, mất trait là CI đỏ, và đó là chủ đích

**Xong khi:** có một endpoint mẫu đủ vòng đời để copy: route → auth → tenant → EF → DTO → test.

### L4 · `retrieval` chạm DB thật · `ingestion` gọi API · `generation` không DB · 4 khối

Đụng: `src/python/{retrieval,ingestion,generation}/**` · `src/python/uv.lock` · `docs/adr/016-*.md`

- **`retrieval`** (D4): thêm `sqlalchemy[asyncio]` + `asyncpg` vào `retrieval/pyproject.toml` (công cụ vẫn khai ở workspace gốc); `GET /retrieval/documents` với `WHERE tenant_id IS NULL OR tenant_id = :tenant` **trong câu SQL**; readiness kiểm kết nối và sự tồn tại của schema `corpus` — **gỡ dòng `TODO(E3)`** đang nói readiness trùng liveness
- **`ingestion`** (D10): `httpx` gọi API của `corpus-service`; readiness kiểm gọi được `/corpus/health/ready`
- **`generation`** (D10): không chạm DB; readiness kiểm `LLM_BASE_URL` có trả lời
- Test trên Postgres thật, có một test khẳng định điều kiện tenant nằm trong chuỗi SQL — đối xứng `Bo_loc_tenant_nam_trong_cau_SQL_chu_khong_o_tang_ung_dung` bên .NET
- `ADR-016` theo D8

**Xong khi:** tắt Postgres → `retrieval` và `ingestion` báo `ready` đỏ, `live` vẫn xanh.

### L5 · `chat` + `generation` nói chuyện với LLM · 4 khối

Đụng: `src/dotnet/Xnk.Chat/**` · `src/python/generation/**` · `docker-compose.yml` · `.env.example` · `docs/adr/015-*.md`

- `Xnk.Chat` theo 9 bước mẫu; `POST /chat/ask` gọi `retrieval` rồi `generation` bằng `IHttpClientFactory` có tên, timeout và retry tường minh
- **Truyền tiếp token của người dùng** xuống service phía sau — không dùng token hệ thống ẩn danh, nếu không lớp cách ly tenant mất tác dụng ngay ở chặng thứ hai
- Profile `llm`: `ollama/ollama` cổng 11434, một one-shot kéo model nhỏ; `LLM_BASE_URL=http://ollama:11434/v1`
- `ADR-015`: một interface, hai cài đặt (OpenAI-compatible local · Bedrock cloud), không có nhánh mock

**Xong khi:** `POST /chat/ask` trả lời sinh từ model local, kèm danh sách văn bản mà `retrieval` đã trả.

### L6 · `workflow-worker` + BPMN thật · 3 khối

Đụng: `src/dotnet/Xnk.WorkflowWorker/**` · `bpmn/*.bpmn` · `docker-compose.yml`

- Worker là **External Task Worker** đăng ký topic với Camunda — Java Delegate bị cấm (`docs/00` §1.2, `ci-bpmn` chặn)
- Một file `.bpmn` thật lấy từ `docs/10`, tối giản nhưng có một bước con người → `ci-bpmn` **hết skip**. Nhớ `.gitattributes`: `*.bpmn` là `eol=lf`
- Worker vào profile `workflow`. CODEOWNERS `/bpmn/` kéo thêm `xnk-expert-team` + `backend-lead`

**Xong khi:** khởi một process instance qua Cockpit `:8090`, worker nhận task và hoàn thành nó.

### L7 · `Xnk.Web` — Blazor WASM · 4 khối

Đụng: `src/dotnet/Xnk.Web/**` · `docker-compose.yml` · renderer nginx (thêm route `/`)

- Blazor WASM thuần (D7); nginx phục vụ `wwwroot` đã publish tại `/`
- Trang đăng nhập gọi `identity-tenant`, lưu token, gắn `Authorization` vào `HttpClient`
- Trang danh sách văn bản gọi `/corpus/documents`
- Xử lý lỗi theo `ProblemDetails` — `AddProblemDetails()` đã bật sẵn ở mọi service
- `ci-blazor` **hết skip**, ngân sách bundle bắt đầu được đo thật. CODEOWNERS `/src/dotnet/Xnk.Web/` cần `frontend-lead`

**Xong khi:** mở `http://localhost:8080/`, đăng nhập hai tài khoản khác tenant → hai danh sách khác nhau.

### L8 · Chốt sổ · 2 khối

- `CONTRIBUTING.md` thêm mục "Chạy toàn bộ hệ thống ở local"
- `docs/16` ma trận truy vết cập nhật cho `E1-11`
- `docs/README.md` chuyển ADR-014/015/016 sang ✅
- Rà `services.json` ↔ đĩa: **9/9**

---

## 4. Kiểm chứng end-to-end — bốn câu, chạy trên máy sạch

```bash
git clone <repo> && cd RAG-XNK && cp .env.example .env
docker compose --profile app up -d
BASE_URL=http://localhost:8080 bash .github/scripts/smoke_test.sh     # ① đạt 7/7 service
```

```bash
# ② hai tenant, hai kết quả khác nhau
TOKEN_A=$(curl -s localhost:8080/identity-tenant/token \
  -H 'Content-Type: application/json' \
  -d '{"email":"...","password":"..."}' | jq -r .access_token)
curl -s localhost:8080/corpus/documents -H "Authorization: Bearer $TOKEN_A" | jq '.items | length'
```

③ Mở `http://localhost:8080/`, đăng nhập hai tài khoản khác tenant, thấy hai danh sách khác nhau.

④ `dotnet test src/dotnet/Xnk.sln` và `cd src/python && uv run pytest` xanh, không cần biến môi
trường nào ngoài `.env`.

**Kiểm âm — phải đỏ đúng chỗ:** `docker compose stop postgres` → `/corpus/health/ready` và
`/retrieval/health/ready` trả **503**, còn `/health/live` vẫn **200**. Readiness mà vẫn xanh thì
luật "không mock" số 3 đã bị vi phạm ở đâu đó.

---

## 5. Công sức và thứ tự cắt

| | Số |
|---|---|
| Tổng | **31 khối 60'** |
| Ở nhịp một khối mỗi ngày | **~4,4 tuần** |
| Sprint 0 theo `docs/01` | 1 tuần |

Chênh lệch đó không phải lỗi của kế hoạch này: `docs/02` biên chế **7 người**, thực tế đang là
**1**. Ai đọc tài liệu Scrum rồi nhìn vào đây thì nên biết con số lệch tới từ đâu.

**Lõi không cắt được: L0 → L3 (14 khối, ~2 tuần).** Hết L3 là một dev .NET đã có mẫu đầy đủ để
nhận story. L4 mở đường cho dev Python; L5–L7 mở đường cho story chat, quy trình và giao diện;
L8 cắt được, đổi lại nợ tài liệu.

---

## 6. Bảy rủi ro đã lường

| # | Rủi ro | Xử |
|---|---|---|
| R1 | Khoá HS256 dùng chung → service nào cũng phát được token | Chỉ local. `ADR-014` chốt hạn chuyển sang JWKS **trước** môi trường thật |
| R2 | `pytest` đặt `filterwarnings = ["error"]` và mypy `strict` | Thêm asyncpg/SQLAlchemy rất dễ làm đỏ CI vì đúng một `DeprecationWarning`. Đã tính thời gian vào L4 |
| R3 | Ollama + Camunda kéo thêm ~5GB ảnh | Giữ cơ chế profile: `up -d` mặc định chỉ Postgres |
| R4 | CPM làm `dotnet add package` hỏng ở SDK 9 | Đã ghi thành bước 3 của mẫu thêm service |
| R5 | PR chạm `/db/migrations/` cần **≥2 approval** + `data-lead` + `backend-lead` | Đội chỉ có 2 account GitHub — tách PR để phần migration đi riêng, đừng gộp vào một PR to |
| R6 | Máy dev Windows | `.sh` phải `eol=lf` (`.gitattributes` đã lo); `apply-migrations.sh` cần Git Bash |
| R7 | Coverage đang đặt **0%** | Không nâng trong đợt này — việc cuối Sprint 1, theo ghi chú trong `quality-gates.yml` |

---

## 7. Việc KHÔNG làm trong đợt này

Ghi ra để không ai lặng lẽ kéo vào giữa chừng:

- **Bất kỳ dòng IaC nào** — CDK/Terraform thuộc `E1-04/05/06`
- **Bật `eval/gates.yml`** — cần bộ đo của `E7-03/E7-04`
- **Tải văn bản pháp luật thật** về `corpus/raw/` — cần chuyên gia xác minh trước
- **Telerik UI** — Sprint 3 (D7)
- **Nâng ngưỡng coverage** — cuối Sprint 1
- **Embedding và vector search thật** — `E2`/`E3`
- **Schema `vector`** — D8
