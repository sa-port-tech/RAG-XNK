# Kế hoạch tổng thể — Hệ thống trợ lý nghiệp vụ Xuất Nhập Khẩu

> Đây là tài liệu kế hoạch duy nhất của dự án. Mọi thay đổi ghi đè trực tiếp lên file này.
> Kế hoạch triển khai prototype theo Scrum: xem [`01-ke-hoach-prototype-scrum.md`](01-ke-hoach-prototype-scrum.md).

---

## 1. Context

**Vấn đề cần giải:** Nghiệp vụ XNK Việt Nam phụ thuộc vào hệ thống văn bản pháp luật dày đặc, thay đổi liên tục, phân tán trên nhiều cơ quan. Nhân viên khai báo hải quan, forwarder và học viên thường mất hàng giờ tra cứu một câu hỏi mà đáp án nằm ở một Khoản trong một Thông tư đã bị sửa đổi hai lần.

**Kết quả mong muốn:** Hệ thống phục vụ ~10.000 tài khoản, trả lời câu hỏi nghiệp vụ XNK **kèm trích dẫn chính xác tới Điều/Khoản/Điểm và ngày hiệu lực**, phân biệt văn bản còn hiệu lực với đã hết hiệu lực, biết từ chối khi không có căn cứ — và **dẫn dắt người dùng qua các quy trình XNK có trạng thái**.

**Rủi ro cốt lõi định hình toàn bộ thiết kế:** Trả lời sai gây thiệt hại thật — tờ khai bị bác, hàng lưu kho phát sinh chi phí, doanh nghiệp bị xử phạt vi phạm hành chính. Một chatbot trích dẫn Thông tư đã hết hiệu lực còn nguy hiểm hơn không có chatbot. Vì vậy **quản lý hiệu lực văn bản theo thời điểm là trục kiến trúc chính**, không phải tính năng phụ.

### 1.1 Quyết định công nghệ đã chốt

| Hạng mục | Prototype (Phase 0.5) | Production |
|---|---|---|
| Kiến trúc | 7 microservice | 7 microservice, tách thêm theo tiêu chí §4.2 |
| Backend | .NET 9 + Python 3.12 | như prototype |
| Frontend | Telerik UI for **Blazor WebAssembly** (trial 30 ngày) | như prototype (license thương mại) |
| Workflow | **Camunda 7 Community Edition** | Camunda 7 Self-Managed |
| LLM | **Amazon Bedrock** (`anthropic.claude-opus-5`) | **Claude Platform on AWS** (`claude-opus-5`) |
| Compute | ECS Fargate **Spot**, single-AZ, không NAT | ECS Fargate, Multi-AZ |
| Database | RDS PostgreSQL single-AZ + pgvector | RDS Multi-AZ, 2 cụm tách biệt |
| Môi trường | 1 (dev) | 3 (dev / staging / production) |
| IaC | AWS CDK (C#) | như prototype |
| CI/CD | GitHub Issues + Projects + Actions + OIDC | như prototype, thêm cổng duyệt |
| Repo | Monorepo + path filter | như prototype |
| Multi-tenant | 3 loại tenant, JWT (hoãn SSO) | 3 loại tenant, SSO SAML/OIDC |

**Lý do đổi LLM giữa hai giai đoạn:** Claude Platform on AWS thanh toán qua AWS Marketplace, mà credit khuyến mãi của AWS theo thông lệ **loại trừ khoản mua qua Marketplace**. Amazon Bedrock là dịch vụ AWS gốc nên credit áp dụng được. Ở prototype, Bedrock vẫn có Citations API và prompt caching thủ công — hai thứ quan trọng nhất. Bedrock thiếu Batches API và automatic prompt caching, nhưng đó là đòn bẩy chi phí ở quy mô lớn, không phải năng lực cốt lõi. Khi lên production và index hàng trăm nghìn chunk thì chuyển sang Claude Platform on AWS để lấy lại Batches API.

### 1.2 Cảnh báo phải xác minh trước khi bắt đầu

1. **Bộ máy hành chính đã thay đổi (2025).** Tổng cục Hải quan được tổ chức lại thành Cục Hải quan trực thuộc Bộ Tài chính; Cục Hải quan tỉnh/thành phố chuyển thành Chi cục Hải quan khu vực. Một số bộ đã sáp nhập. Rà lại danh mục cơ quan ban hành trước khi thiết kế schema.
2. **VNACCS/VCIS đang trong lộ trình thay thế.** Tách nội dung VNACCS thành collection riêng, gắn cờ độ ổn định thấp.
3. **Số hiệu văn bản trong tài liệu này chỉ là ví dụ minh họa.** Trạng thái hiệu lực phải lấy tự động từ nguồn chính thức.
4. **Camunda 7 đang trong lộ trình kết thúc hỗ trợ.** Lựa chọn có chủ đích. Biện pháp bắt buộc: chỉ dùng **External Task pattern**, không Java Delegate, không script nhúng trong BPMN.
5. **Điều khoản AWS Free Tier và credit đã thay đổi trong 2025.** Kiểm chứng hạn mức và điều khoản loại trừ Marketplace **ngay tuần đầu tiên**, trước khi lập ngân sách.

---

## 2. Ba nguyên tắc bất di bất dịch

Mọi thiết kế phía dưới phục vụ ba nguyên tắc này. Khi có xung đột giữa tính năng và nguyên tắc, nguyên tắc thắng.

1. **Mọi khẳng định phải có trích dẫn** tới Điều/Khoản/Điểm kèm ngày hiệu lực.
2. **Không có căn cứ thì từ chối trả lời** — cấm trả lời từ kiến thức nền của model.
3. **Không khẳng định mã HS** — chỉ tra cứu tham khảo; phân loại chính thức thuộc thẩm quyền cơ quan hải quan.

---

## 3. Phase 0.5 — Prototype trên AWS Free Tier

Mục tiêu: trả lời câu hỏi *"kiến trúc này có hoạt động không, và có nên đầu tư ~$12.200/tháng cho production không?"* với chi phí hạ tầng bằng 0.

### 3.1 Phạm vi

| Hạng mục | Quyết định | Lý do |
|---|---|---|
| 7 microservice | ✅ **Giữ nguyên** | Không phải làm cuộc tách monolith sau này. Đổi bằng tốc độ phát triển (~1–2 tuần), không đổi bằng tiền. |
| 3 loại tenant + cách ly `tenant_id` | ✅ **Giữ nguyên** | Multi-tenancy là **ranh giới bảo mật**, nhét vào sau là việc rủi ro cao. Chi phí hạ tầng bằng 0. |
| SSO SAML/OIDC | ⏸ Hoãn — JWT đơn giản | Phần đắt là đấu nối IdP của từng khách hàng, cần khách hàng thật. Được 90% giá trị với 20% công sức. |
| OCR | ✅ Giữ, **dạng spike đo lường** | Xem §3.2 |
| CI/CD 1 môi trường (dev) | ✅ Giữ, luồng GitHub đầy đủ | Bỏ cổng duyệt staging/production, giữ nguyên luồng Issue → branch → PR → deploy |
| Corpus | ✂️ **15 văn bản** | Trục: Luật Hải quan → NĐ 08/2015 → NĐ 59/2018 → **TT 38/2015 → TT 39/2018** |
| Camunda | ✂️ Chỉ quy trình P1, rút gọn | |
| Golden set | ✂️ 40–50 câu, **≥10 câu bẫy hiệu lực** | |
| Multi-AZ / HA / blue-green | ✂️ Bỏ | |
| X-Ray | ✂️ OpenTelemetry → Jaeger container | Miễn phí, đủ cho 7 service |
| **Model Claude** | ✅ **Opus 5, không hạ** | Xem §3.4 |
| **Citations API** | ✅ Giữ | |
| **Bộ lọc hiệu lực** | ✅ Giữ | Là thứ cần chứng minh nhất |

**Vì sao chọn đúng bộ 15 văn bản này:** TT 39/2018 sửa đổi *một phần* TT 38/2015. Đây chính là ca kinh điển của vấn đề kiến trúc quan trọng nhất — văn bản vẫn hiển thị "còn hiệu lực" nhưng nhiều Điều bên trong đã bị thay thế. Chỉ với hai văn bản này đã chứng minh được (hoặc bác bỏ) luận điểm cốt lõi của cả hệ thống.

### 3.2 OCR là spike đo lường, không phải mắt xích pipeline

OCR miễn phí về tiền nhưng có thể nuốt trọn 1–2 tuần mà không tạo ra kết luận nào. Cách làm:

- Chọn 3–5 văn bản scan tiêu biểu, ưu tiên phụ lục biểu mẫu và danh mục hàng hóa (chỗ khó nhất)
- Chạy song song **VietOCR**, **PaddleOCR-vi**, **Amazon Textract**
- Đo hai con số: độ chính xác ký tự, và **thời gian chuyên gia phải sửa lại** — con số thứ hai mới dùng để lập kế hoạch Phase 1
- Đóng gói thành báo cáo, **không nối vào pipeline chính**

15 văn bản của corpus chính chọn loại **có sẵn text layer**. Lý do: nếu retrieval và bộ lọc hiệu lực bị chặn vì OCR chưa xong, prototype mất khả năng trả lời câu hỏi quan trọng nhất của nó.

> OCR trả lời câu *"Phase 1 tốn bao nhiêu công?"*. Retrieval + lọc hiệu lực trả lời câu *"Có nên làm Phase 1 không?"*. Đừng để câu đầu chặn câu sau.

### 3.3 Cấu hình AWS tối thiểu

| | Cấu hình | Ghi chú |
|---|---|---|
| Compute | ECS **Fargate Spot**, 7 task | Giảm ~70%. `retrieval` cần 1 vCPU / 2–4GB (BGE-M3 + reranker); còn lại 0.25–0.5 vCPU / 0.5–1GB |
| Mạng | Public subnet + Security Group siết theo IP văn phòng | **Không NAT Gateway** — tiết kiệm ~$32/tháng/AZ |
| Database | RDS PostgreSQL `db.t4g.small` single-AZ + pgvector | |
| Camunda DB | RDS `db.t4g.micro` single-AZ | |
| Cache | ElastiCache `t4g.micro` | |
| Lưu trữ | S3 (5GB free tier) cho file gốc | Bật Versioning; Object Lock để sau |
| LLM | **Amazon Bedrock** — `anthropic.claude-opus-5` | Phải **bật quyền truy cập model** trong Bedrock console theo region |
| Quan sát | CloudWatch (free tier) + Jaeger container | |

### 3.4 Cái không được cắt

**① Chất lượng model.** Đây là cái bẫy đắt nhất. Prototype chạy trên model yếu sẽ trích dẫn sai, bịa điều khoản, và đội ngũ sẽ kết luận *"RAG cho pháp luật không đủ tin cậy"* — một kết luận sai gây ra bởi thay nhầm biến số. Chênh lệch giữa Opus 5 và model rẻ ở quy mô prototype là **vài chục đô la**; chênh lệch trong kết luận rút ra là cả dự án.

**② Citations API.** Rời khỏi Claude để dùng free tier nhà cung cấp khác là mất tính năng này, phải tự bảo LLM viết `[1][2]` trong prompt — mà đó chính xác là chỗ trích dẫn bịa xuất hiện.

**③ Bộ lọc hiệu lực.** Là thứ cần chứng minh nhất, không phải thứ để cắt.

**④ Chuyên gia XNK chấm điểm.** Dù chỉ 40 câu, vẫn cần người biết nghiệp vụ đánh giá. Không có bước này thì prototype không trả lời được câu hỏi duy nhất nó sinh ra để trả lời.

### 3.5 Kiểm soát chi phí — sáu việc bắt buộc

Credit hết vì lãng phí là kịch bản phổ biến, luôn do cùng vài nguyên nhân:

1. **Đừng để wizard tạo NAT Gateway.** Trình tạo VPC mặc định tạo NAT Gateway mỗi AZ — ba AZ là ~$96/tháng đốt cho thứ không cần. **Kiểm tra lại tab NAT Gateways sau khi tạo VPC.**
2. **Tắt tài nguyên ngoài giờ.** Giờ làm việc là 45/168 giờ mỗi tuần → tiết kiệm ~65%. EventBridge Scheduler: `UpdateService --desired-count 0` cho Fargate, `StopDBInstance` cho RDS. *RDS chỉ stop tối đa 7 ngày rồi tự khởi động lại — lịch phải chạy đều.*
3. **Fargate Spot** cho toàn bộ môi trường dev.
4. **AWS Budgets + cảnh báo** ở $50 / $100 / $150. Bật **Cost Anomaly Detection**.
5. **Tag mọi tài nguyên** `Project=rag-xnk`, `Env=dev` ngay trong CDK. Không tag thì Cost Explorer không cho biết tiền đi đâu.
6. **Guardrail chi tiêu LLM trong code.** Prototype dễ đốt tiền vì một vòng lặp lỗi gọi API hàng nghìn lần. Đếm token theo tenant, hạn mức cứng mỗi ngày, log `usage` mọi request.

### 3.6 Chi phí prototype

| | Chạy 24/7 | Tắt ngoài giờ |
|---|---|---|
| Hạ tầng AWS | ~$130/tháng | **~$45/tháng** |
| LLM (500–1.000 truy vấn) | $33–65 tổng | $33–65 tổng |
| **Tổng 7 tuần** | ~$260 | **~$150** |

Với credit ~$200 (mô hình mới của AWS: $100 khi đăng ký + tối đa $100 từ activity — *cần kiểm chứng*), toàn bộ prototype nằm trong phạm vi credit. **Chi phí hạ tầng tự bỏ ra: $0.**

### 3.7 Tiêu chí Go / No-Go

**GO** khi đạt tất cả:
- `Stale Citation Rate = 0` trên bộ 10 câu bẫy
- `Recall@10 ≥ 0,90` trên golden set 40–50 câu
- Chuyên gia XNK chấm **≥ 4,0/5** trên nhóm câu dễ + trung bình
- Test cách ly tenant pass
- Demo đầu-cuối chạy được: hỏi → trả lời có trích dẫn → bấm chip nhảy đúng đoạn văn bản gốc
- CI/CD tự động deploy lên dev hoạt động
- Báo cáo OCR có số liệu để ước lượng Phase 1

**NO-GO tuyệt đối** nếu `Stale Citation Rate > 0` hoặc chuyên gia chấm < 3,5 — đây là luận điểm cốt lõi, không thương lượng.

> $200 credit so với $12.200/tháng ở production tương đương **12 giờ vận hành**. Credit là công cụ để trả lời câu hỏi kiến trúc với chi phí bằng 0, không phải mô hình kinh doanh. Nếu prototype cho thấy chỉ số không đạt, bạn tiết kiệm được nhiều hơn $12k rất nhiều.

---

## 4. Kiến trúc tổng thể

### 4.1 Sơ đồ hệ thống

```
                          ┌──────────────┐
   Người dùng ──────────► │  CloudFront  │ ──► S3 (Blazor WASM + Telerik)
                          └──────┬───────┘
                                 │ HTTPS/JSON + SSE
                          ┌──────▼───────┐
                          │  WAF → ALB   │
                          └──────┬───────┘
        ┌────────────────────────┼────────────────────────┐
        │                ECS Fargate                      │
        │  .NET 9                        Python 3.12       │
        │  ┌────────────────┐            ┌──────────────┐  │
        │  │ identity-tenant│            │  retrieval   │  │
        │  ├────────────────┤            ├──────────────┤  │
        │  │ chat-orchestr. │◄──────────►│  generation  │──┼──► Claude
        │  ├────────────────┤            ├──────────────┤  │   (Bedrock / 
        │  │ corpus-service │            │  ingestion   │  │    Platform on AWS)
        │  ├────────────────┤            └──────┬───────┘  │
        │  │ workflow-worker│◄───────────────────┤          │
        │  └───────┬────────┘   External Task    │          │
        │  ┌───────▼────────┐   (REST fetch&lock)│          │
        │  │  Camunda 7 Run │ (Java, Spring Boot)│          │
        │  └───────┬────────┘                    │          │
        └──────────┼─────────────────────────────┼──────────┘
     ┌─────────────▼──┐  ┌──────────────┐  ┌────▼─────┐  ┌───────────┐
     │ RDS PG         │  │ RDS PG       │  │ S3       │  │ElastiCache│
     │ (Camunda)      │  │ (+pgvector)  │  │(file gốc)│  │  Redis    │
     └────────────────┘  └──────────────┘  └──────────┘  └───────────┘

  Bất đồng bộ: SQS (ingest/embed) · EventBridge (scheduler + event bus) · SNS
```

### 4.2 Phân rã microservice

**Nguyên tắc tách:** theo *ranh giới nghiệp vụ và nhịp thay đổi*, không theo tầng kỹ thuật. Chỉ tách thêm khi có **lý do đo được**: đội ngũ sở hữu khác nhau, nhịp deploy khác nhau, đặc tính scale khác nhau, hoặc yêu cầu tuân thủ khác nhau.

**.NET 9 (ASP.NET Core)**

| Service | Trách nhiệm | Sở hữu dữ liệu |
|---|---|---|
| `identity-tenant` | Xác thực (JWT ở prototype, SSO ở production), tổ chức, người dùng, RBAC, quota, rate limit | schema `identity`, `tenant` |
| `chat-orchestrator` | Vòng đời hội thoại, streaming SSE ra client, điều phối `retrieval` → `generation`, khởi tạo/truy vấn process instance Camunda | schema `conversation` |
| `corpus-service` | CRUD văn bản, hàng đợi review, API tra cứu có cấu trúc (HS, biểu thuế, mã loại hình, Incoterms), admin API | schema `corpus`, `lookup` |
| `workflow-worker` | External Task Worker cho các bước .NET trong BPMN | không sở hữu |

**Python 3.12 (FastAPI)**

| Service | Trách nhiệm | Sở hữu dữ liệu |
|---|---|---|
| `ingestion` | Crawler, parser PDF/DOCX, OCR, phân rã Điều/Khoản, trích metadata hiệu lực, dò quan hệ sửa đổi. Đồng thời là External Task Worker | ghi qua `corpus-service` API |
| `retrieval` | Embedding (BGE-M3 ONNX), hybrid search BM25 + vector, **lọc hiệu lực**, rerank | đọc `corpus`, `vector` |
| `generation` | Gọi Claude (citations, prompt caching, tool calling), guardrails XNK, chạy eval | không sở hữu |

### 4.3 Giao tiếp giữa các service

| Loại | Cơ chế | Dùng khi |
|---|---|---|
| Đồng bộ | REST/JSON qua ECS Service Connect | Luồng hỏi–đáp, cần kết quả ngay |
| Hàng đợi | Amazon SQS | Sinh embedding hàng loạt, crawl, OCR |
| Sự kiện | Amazon EventBridge | `corpus.document.approved`, `corpus.document.superseded`, `feedback.reported` |
| Quy trình dài | Camunda 7 External Task | Có bước con người, trạng thái bền vững, timer, escalation |
| Định kỳ | EventBridge Scheduler | Crawl hàng tuần, regression hàng đêm |

**Quy tắc chọn:** cần trả lời trong một request HTTP → đồng bộ. Tác vụ nền không có con người → SQS. **Có bước con người, timer, hoặc rẽ nhánh nghiệp vụ** → Camunda.

### 4.4 Sở hữu dữ liệu

Một cụm RDS PostgreSQL với **schema tách biệt theo service**:

- Mỗi service có DB user riêng, chỉ được cấp quyền trên schema của mình
- Service khác muốn đọc → gọi API, không JOIN chéo schema
- **Ngoại lệ có chủ đích:** `retrieval` được cấp quyền **đọc** trực tiếp `corpus` và `vector`, vì lọc hiệu lực **phải nằm trong cùng một câu SQL** với vector search — tách ra sẽ phá lớp phòng thủ quan trọng nhất của hệ thống (§10.3). Đây là đánh đổi được cân nhắc, không phải sơ suất.
- Camunda 7 dùng **cụm RDS riêng hoàn toàn** — engine DB có mô hình khoá và nhịp ghi rất khác.

---

## 5. Frontend — Blazor WebAssembly + Telerik

### 5.1 Bắt buộc WebAssembly, không dùng Blazor Server

Với 10.000 tài khoản, Blazor Server là lựa chọn sai: mỗi người dùng đang mở tab giữ một kết nối SignalR thường trực cộng state phía server. Chi phí và độ phức tạp tăng theo số phiên đồng thời, và mọi gián đoạn mạng làm mất phiên làm việc.

Blazor WebAssembly chạy hoàn toàn trong trình duyệt, gọi REST như SPA thông thường: backend scale theo số **request** thay vì số **tab đang mở**, host tĩnh trên S3 + CloudFront gần như miễn phí, Telerik hỗ trợ đầy đủ.

### 5.2 Streaming câu trả lời

`HttpClient` đọc response stream SSE từ `chat-orchestrator`, đẩy từng đoạn vào `IAsyncEnumerable<string>`, component gọi `StateHasChanged()` có throttle ~50ms.

**Dựng prototype phần này ngay Sprint 1** — không để tới cuối mới phát hiện vướng.

### 5.3 Màn hình và component

| Màn hình | Component Telerik |
|---|---|
| Chat | `TelerikCard`, `TelerikSkeleton` |
| Panel văn bản gốc | `TelerikSplitter`, `TelerikPdfViewer` |
| Quản lý corpus | `TelerikGrid` (virtual scroll, filter, group), `TelerikFilter` |
| Hàng đợi review chuyên gia | `TelerikGrid` + `TelerikWindow` |
| Theo dõi quy trình XNK | `TelerikStepper`, `TelerikTimeline` |
| Dashboard chi phí & chất lượng | `TelerikChart`, `TelerikGauge` |
| Tra cứu HS / biểu thuế | `TelerikTreeList`, `TelerikAutoComplete` |

Yêu cầu UX riêng của domain:
- **Badge trạng thái hiệu lực** cạnh mỗi trích dẫn: xanh (còn hiệu lực) / vàng (hết hiệu lực một phần) / đỏ (hết hiệu lực)
- **Trích dẫn dạng chip bấm được** → mở panel văn bản gốc, cuộn tới và highlight đúng đoạn
- **Nút "Báo cáo sai sót"** ở mỗi câu trả lời → khởi tạo quy trình P2
- Responsive — nhân viên tra cứu bằng điện thoại tại cảng/kho là kịch bản thật

**Cấp phép:** license key nằm trong AWS Secrets Manager, tiêm vào lúc build trong GitHub Actions, **không commit vào repo**. Prototype dùng trial 30 ngày — kích hoạt đúng đầu Sprint 3.

---

## 6. Camunda 7 — dùng ở đâu, và KHÔNG dùng ở đâu

### 6.1 Mô hình triển khai

- **Camunda Run** (Spring Boot standalone, bật REST API) chạy như ECS Fargate service, backed bởi RDS riêng. Prototype dùng **Community Edition** (Apache 2.0, miễn phí, có đủ engine + Cockpit + Tasklist).
- **Chỉ dùng External Task pattern.** Worker .NET/Python gọi `fetch-and-lock` qua REST. Không Java Delegate, không script nhúng. Vừa là điều kiện kỹ thuật để service .NET/Python tham gia được, vừa là biện pháp giảm rủi ro EOL.
- **Cockpit/Admin** chỉ cho vận hành, sau ALB nội bộ + VPN.
- **Tasklist mặc định KHÔNG dùng.** User Task render bằng UI Blazor + Telerik của hệ thống (gọi Camunda REST). Chuyên gia XNK làm việc trong một giao diện thống nhất.
- File `.bpmn` **nằm trong git**, deploy qua pipeline CI/CD.

### 6.2 Bốn quy trình xứng đáng

#### P1 — Đưa văn bản mới vào corpus *(quan trọng nhất)*

```
Start ← EventBridge Scheduler phát hiện văn bản mới
  → [Service] Tải & lưu file gốc vào S3                        [python:ingestion]
  → [Service] Trích xuất text / OCR                            [python:ingestion]
  → [Service] Phân rã Điều/Khoản/Điểm                          [python:ingestion]
  → <Gateway> Độ tin cậy parser ≥ 98%?
       ├─ không → [User Task] Chuyên gia sửa cấu trúc thủ công
       └─ có ────┐
  → [Service] Trích xuất metadata hiệu lực                     [python:ingestion]
  → [Service] Dò quan hệ sửa đổi/thay thế                      [python:ingestion]
  → <Gateway> Có sửa đổi văn bản đang có trong corpus?
       └─ có → [Service] Gắn cờ cảnh báo các chunk bị ảnh hưởng [dotnet:worker]
              → [User Task] Chuyên gia review phạm vi ảnh hưởng   ★ BẮT BUỘC
  → [User Task] Chuyên gia phê duyệt đưa vào corpus
  → [Service] Sinh embedding & index                            [python:retrieval]
  → [Service] Chạy regression trên golden set                   [python:generation]
  → <Gateway> Chỉ số suy giảm?
       ├─ có → [User Task] Điều tra → [Service] Rollback index
       └─ không → [Service] Kích hoạt trên production
End
```

**Timer boundary event** trên mỗi User Task: quá 5 ngày làm việc → escalate lên trưởng nhóm chuyên môn; quá 10 ngày → thông báo quản lý dự án. Đây là thứ không thể làm gọn bằng cron + hàng đợi, và là lý do Camunda có mặt.

★ Bước review phạm vi ảnh hưởng là **cổng chất lượng không được bỏ qua** — sai ở đây làm sai vĩnh viễn mọi câu trả lời sau đó.

#### P2 — Xử lý báo cáo sai sót từ người dùng

```
Start ← người dùng bấm "Báo cáo sai sót"
  → [Service] LLM phân loại: lỗi retrieval / dữ liệu / guardrail / không phải lỗi
  → <Gateway> Mức độ nghiêm trọng
       ├─ NGHIÊM TRỌNG (trích dẫn văn bản hết hiệu lực)
       │    → [Service] Tạm vô hiệu hoá chunk liên quan NGAY
       │    → [User Task] Xử lý trong 24h  (timer quá hạn → gọi on-call)
       └─ thường → [User Task] Hàng đợi review hàng tuần
  → [User Task] Chuyên gia xác nhận & sửa
  → [Service] Bổ sung câu hỏi vào golden set
  → [Service] Chạy regression
  → [Service] Thông báo kết quả cho người báo cáo
End
```

#### P3 — Dẫn dắt quy trình nghiệp vụ XNK có trạng thái *(điểm khác biệt sản phẩm)*

Nâng hệ thống từ "chatbot hỏi–đáp" thành "trợ lý quy trình". Người dùng hỏi *"tôi cần làm gì để xin xác định trước mã số hàng hóa?"* → hệ thống trả lời **và** đề nghị mở quy trình theo dõi. Mỗi lần quay lại, hệ thống biết họ đang ở bước nào, thiếu chứng từ gì, deadline nào sắp tới.

Ba quy trình cho v1: **xin xác định trước mã số hàng hóa** · **chuẩn bị bộ chứng từ khai báo cho một lô hàng** · **xin C/O theo từng FTA**.

Kỹ thuật: `chat-orchestrator` khởi tạo process instance qua Camunda REST, lưu `businessKey` gắn với người dùng và lô hàng. UI dùng `TelerikStepper`.

#### P4 — Onboarding tenant B2B

Ký hợp đồng → tạo tổ chức → cấu hình SSO → nạp corpus riêng → đào tạo → kích hoạt. Có phê duyệt và timer nhắc việc.

### 6.3 KHÔNG dùng Camunda ở những chỗ này

| Luồng | Vì sao không | Dùng gì |
|---|---|---|
| **Hỏi–đáp thường** | Request/response đồng bộ dưới 10 giây, không có bước con người | REST trực tiếp giữa service |
| **Sinh embedding hàng loạt** | Thuần kỹ thuật, không rẽ nhánh nghiệp vụ | SQS + worker |
| **Crawl định kỳ** | Chỉ là lịch chạy | EventBridge Scheduler — chỉ khi *phát hiện văn bản mới* mới khởi tạo P1 |
| **Retry khi gọi API lỗi** | Mối quan tâm của tầng hạ tầng | Polly (.NET) / tenacity (Python) + DLQ |
| **Điều phối request giữa microservice** | Camunda không phải service mesh | ECS Service Connect |

---

## 7. Hạ tầng AWS

| Nhóm | Dịch vụ | Vai trò |
|---|---|---|
| **Mạng** | VPC, subnet, VPC Endpoints (S3, ECR, Secrets Manager, CloudWatch, Bedrock) | Endpoint giảm chi phí NAT và giữ traffic trong mạng AWS |
| **Compute** | ECS Fargate, ALB, ECS Service Connect | |
| **Dữ liệu** | RDS PostgreSQL 16 + `pgvector` (nghiệp vụ) · RDS PostgreSQL (Camunda) · ElastiCache Redis | Redis: semantic cache, session, rate limit |
| **Lưu trữ** | S3 + **Versioning + Object Lock** (file gốc) · S3 (Blazor WASM, artifact) | Object Lock là bằng chứng gốc bất biến khi tranh chấp về nội dung văn bản |
| **Bất đồng bộ** | SQS (+ DLQ), EventBridge (bus + scheduler), SNS | |
| **AI** | Bedrock (prototype) → Claude Platform on AWS (production) | |
| **Bảo mật** | IAM task role riêng từng service, Secrets Manager, KMS, WAF, Security Group tối thiểu | Không service nào dùng chung task role |
| **Quan sát** | CloudWatch Logs/Metrics/Alarms · X-Ray (production) / Jaeger (prototype) | Bắt buộc: một câu hỏi đi qua 3–4 service |
| **Phân phối** | CloudFront + S3, ECR | |
| **IaC** | **AWS CDK (C#)** | Cùng ngôn ngữ với team .NET |

**Môi trường production:** ba AWS account tách biệt qua AWS Organizations (`dev` · `staging` · `production`). GitHub Actions truy cập từng account qua **OIDC federation với IAM role riêng biệt**.

**Embedding — tránh GPU thường trực:**
- **Lúc truy vấn**: BGE-M3 dạng **ONNX Runtime quantized trên CPU** trong service `retrieval`. Độ trễ ~30–50ms.
- **Lúc index hàng loạt**: **EC2 g5.xlarge Spot**, khởi tạo theo yêu cầu từ quy trình P1, tự tắt khi xong.

Cách này loại bỏ khoản GPU thường trực ~$500/tháng mà không đánh đổi chất lượng.

---

## 8. Quy trình phát triển & CI/CD trên GitHub

### 8.1 Cấu trúc monorepo

```
.github/
  ISSUE_TEMPLATE/{bug,feature,corpus-issue,expert-review}.yml
  PULL_REQUEST_TEMPLATE.md · CODEOWNERS
  workflows/{ci-dotnet,ci-python,ci-blazor,ci-bpmn,eval-regression,cd-deploy,project-automation}.yml
src/
  dotnet/  Xnk.IdentityTenant · Xnk.Chat · Xnk.Corpus · Xnk.WorkflowWorker
           Xnk.Web (Blazor+Telerik) · Xnk.Shared
  python/  ingestion · retrieval · generation
bpmn/        file .bpmn (versioned)
prompts/     system prompt (versioned)
db/migrations/
eval/golden-set/
infra/       AWS CDK (C#)
docs/
```

**Prompt và BPMN nằm trong git là quyết định có chủ đích.** Thay đổi system prompt là thay đổi hành vi hệ thống — phải qua PR và phải chạy regression eval, giống hệt thay đổi code.

**Nội dung corpus KHÔNG nằm trong git.** Văn bản pháp luật đi qua quy trình P1, lưu trong RDS/S3.

### 8.2 Quản lý ticket bằng GitHub (thay Jira)

**Issues + Issue Forms** — mỗi template là file YAML với trường có cấu trúc (dropdown, checkbox), không phải markdown tự do; dữ liệu vào Project board mới dùng được.

**GitHub Projects (v2)** làm board: `Backlog → Todo → In Progress → In Review → Testing → Done`

Custom fields: `Priority` (P0–P3) · `Component` (ingestion/retrieval/generation/web/infra/bpmn) · `Phase` · `Story Points` · `Iteration` (sprint 2 tuần) · `Expert Review Required` (boolean).

### 8.3 Luồng đầy đủ: từ ticket đến deploy

```
① TẠO TICKET — Issue Form → tự thêm vào Project cột "Backlog"

② SPRINT PLANNING — kéo sang "Todo", gán assignee + Iteration

③ TẠO BRANCH TỪ ISSUE
   Bấm "Create a branch" ngay trên Issue (tính năng native GitHub)
   → branch `123-them-loc-hieu-luc`, liên kết hai chiều với issue
   → automation chuyển issue sang "In Progress"

④ PHÁT TRIỂN & MỞ PR
   PR template có sẵn `Closes #123` → issue sang "In Review"
   → CODEOWNERS tự gán reviewer
   → CI chạy, CHỈ cho service bị đổi (path filter):
        ci-dotnet      : build, unit test, format, phân tích tĩnh
        ci-python      : ruff, mypy, pytest
        ci-blazor      : build WASM, kiểm tra kích thước bundle
        ci-bpmn        : validate BPMN, CHẶN Java Delegate lọt vào
        CodeQL         : quét bảo mật
        eval-regression: CHỈ khi đụng prompts/, retrieval/, generation/, eval/

⑤ CỔNG CHẤT LƯỢNG (required status checks)
   · Toàn bộ test xanh · coverage không giảm
   · ≥1 approval (≥2 nếu đụng infra/ hoặc db/migrations/)
   · Test cách ly tenant pass
   · Nếu chạy eval-regression:
       Stale Citation Rate = 0     ← chặn merge tuyệt đối
       Recall@10 ≥ 0,90
       Không chỉ số nào tụt quá 2% so với baseline trên main
     Bot comment bảng so sánh trước/sau vào PR

⑥ MERGE — squash merge vào main → issue tự đóng → sang "Testing"

⑦ TRIỂN KHAI TỰ ĐỘNG (cd-deploy.yml)
   → Build image, tag {semver}-{git-sha} → push ECR
   → Deploy DEV (tự động) → smoke test
   [Production thêm:]
   → Deploy STAGING → integration test + eval đầy đủ
   → ⏸ CHỜ DUYỆT THỦ CÔNG (GitHub Environment protection rule)
   → Deploy PRODUCTION (blue/green qua CodeDeploy, tự rollback theo CloudWatch Alarm)
   → issue sang "Done" · tạo GitHub Release kèm changelog
```

### 8.4 Branch protection (Rulesets trên `main`)

Cấm push trực tiếp · bắt buộc PR và status check · linear history (squash merge) · giải quyết hết comment trước khi merge · signed commits · cấm force push và xoá nhánh.

### 8.5 Xác thực GitHub Actions → AWS

**OIDC federation, tuyệt đối không dùng access key dài hạn.** Mỗi môi trường một IAM role, trust policy giới hạn theo repo **và** theo environment:

```
repo:<org>/<repo>:environment:production → XnkGitHubDeployProduction
repo:<org>/<repo>:environment:staging    → XnkGitHubDeployStaging
repo:<org>/<repo>:ref:refs/heads/main    → XnkGitHubDeployDev
```

Role production chỉ assume được từ GitHub Environment `production`, mà environment đó có required reviewers — **không có đường deploy thẳng lên production mà không qua người duyệt**, kể cả khi ai đó sửa được workflow file.

### 8.6 CODEOWNERS

```
/prompts/            @xnk-expert-team @ai-lead
/eval/golden-set/    @xnk-expert-team
/bpmn/               @xnk-expert-team @backend-lead
/db/migrations/      @data-lead @backend-lead
/infra/              @devops-lead @backend-lead
```

---

## 9. Pipeline dữ liệu

### 9.1 Thu thập

| Nguồn | Nội dung | Giá trị |
|---|---|---|
| `vbpl.vn` (CSDL quốc gia về VBPL — Bộ Tư pháp) | Toàn văn + **trạng thái hiệu lực chính thức** + quan hệ sửa đổi | ⭐ Nguồn gốc cho metadata hiệu lực |
| `vanban.chinhphu.vn` | Luật, Nghị định | Cao |
| `customs.gov.vn` | Thông tư, công văn hướng dẫn hải quan | Cao (đặc biệt công văn) |
| `mof.gov.vn` | Thông tư BTC, biểu thuế | Cao |
| `moit.gov.vn` | C/O, quy tắc xuất xứ, giấy phép | Cao |
| `vnsw.gov.vn` | Danh mục thủ tục kiểm tra chuyên ngành | Trung bình |

Yêu cầu: tôn trọng `robots.txt` · ≤ 1 req/2s/domain · `User-Agent` định danh kèm email liên hệ · lưu **file gốc bất biến** vào S3 kèm `sha256` · idempotent.

*Prototype: tải thủ công 15 văn bản. Crawler là công việc của Phase 1.*

### 9.2 Trích xuất

- PDF có text layer: `pdfplumber` / `PyMuPDF`
- PDF scan: VietOCR / PaddleOCR-vi / Amazon Textract — chọn theo kết quả trên mẫu thật (§3.2)
- DOCX: `python-docx`
- **Bảng biểu là điểm yếu chí mạng.** Biểu thuế và Danh mục hàng hóa là bảng nhiều nghìn dòng; trích sai = trả sai thuế suất. Nhóm này trích riêng thành bảng có cấu trúc (§10.4), **bắt buộc chuyên gia đối chiếu 100%**.

### 9.3 Phân rã cấu trúc pháp lý

```
Phần → Chương → Mục → Tiểu mục → Điều → Khoản → Điểm
```

1. **Regex là lớp thứ nhất** — `^Điều\s+(\d+)\.`, `^(\d+)\.\s`, `^([a-zđ])\)\s`, `^Chương\s+([IVXLC]+)`, `^Mục\s+(\d+)`
2. **LLM là lớp kiểm chứng** — chỉ cho đoạn regex không khớp hoặc mơ hồ. Không dùng LLM cho toàn bộ: tốn kém và kém ổn định hơn regex ở phần cấu trúc rõ ràng.
3. **Kiểm tra toàn vẹn** — số Điều liên tục, đối chiếu mục lục. Lỗi → User Task trong P1.

Kết quả: `38/2015/TT-BTC > Chương II > Mục 3 > Điều 18 > Khoản 2 > Điểm b`

### 9.4 Metadata & Đồ thị hiệu lực *(hạng mục quan trọng nhất)*

```
van_ban:
  id, so_hieu, loai_van_ban, trich_yeu
  co_quan_ban_hanh, nguoi_ky, ngay_ban_hanh
  ngay_hieu_luc, ngay_het_hieu_luc (nullable)
  trang_thai: chua_co_hieu_luc | con_hieu_luc | het_hieu_luc_mot_phan
              | het_hieu_luc | ngung_hieu_luc
  linh_vuc[], nguon_url, file_hash, s3_key, ngay_crawl

dieu_khoan:
  van_ban_id, duong_dan_cau_truc, so_dieu, so_khoan, so_diem, noi_dung
  ngay_hieu_luc_hieu_dung      ← có thể KHÁC ngày hiệu lực của cả văn bản
  ngay_het_hieu_luc_hieu_dung
  trang_thai_dieu_khoan

sua_doi:   (cạnh của đồ thị)
  van_ban_sua_doi_id → van_ban_bi_sua_doi_id
  loai: sua_doi | bo_sung | bai_bo | thay_the | dinh_chinh | hop_nhat
  pham_vi_anh_huong: [danh sách dieu_khoan_id bị tác động]
  ngay_hieu_luc_cua_sua_doi
```

**Tại sao phải theo dõi hiệu lực ở cấp Điều/Khoản:** Trường hợp phổ biến nhất trong lĩnh vực hải quan là một Thông tư sửa đổi *một phần* Thông tư khác. Văn bản gốc vẫn "còn hiệu lực" ở cấp văn bản nhưng nhiều Điều bên trong đã bị thay thế. Chỉ lọc theo trạng thái cấp văn bản thì hệ thống **vẫn trả về nội dung sai** trong khi trạng thái hiển thị là "còn hiệu lực".

**Ưu tiên văn bản hợp nhất (VBHN)** khi cơ quan ban hành đã phát hành — giảm đáng kể độ phức tạp của đồ thị.

**Cách xây:** lấy trạng thái hiệu lực trực tiếp từ `vbpl.vn` (không tự suy luận) → LLM trích phạm vi ảnh hưởng cấp Điều/Khoản từ nội dung văn bản sửa đổi → **chuyên gia review 100%** trong User Task của P1. Hàm truy vấn: `hieu_luc_tai_ngay(dieu_khoan_id, ngay) → bool`.

### 9.5 Cổng chất lượng dữ liệu

- [ ] Chuyên gia review ngẫu nhiên ≥ 10% số Điều đã phân rã, độ chính xác cấu trúc ≥ 98%
- [ ] 100% bảng biểu thuế và danh mục HS đối chiếu thủ công với bản gốc
- [ ] Đồ thị hiệu lực được review 100%
- [ ] Không văn bản nào thiếu `ngay_hieu_luc`
- [ ] Toàn bộ câu bẫy trả lời đúng **bằng truy vấn SQL thuần**, chưa cần LLM

---

## 10. Truy xuất

### 10.1 Chunking

**Không dùng fixed-size chunking.** Đơn vị chunk là **Điều** — đơn vị viện dẫn tự nhiên trong nghiệp vụ.

- Điều ngắn (< 1000 token): 1 chunk = 1 Điều
- Điều dài: tách theo Khoản, **mỗi chunk mang header ngữ cảnh** (tên văn bản, Chương, Mục, tiêu đề Điều) — thiếu thì chunk mất hoàn toàn ý nghĩa pháp lý
- Bảng biểu: **không chunk** (§10.4)
- Lưu `parent_dieu_id` để mở rộng lên toàn Điều khi cần

### 10.2 Hybrid search

**BM25 / full-text (không thể bỏ)** — người dùng gõ định danh chính xác: `38/2015/TT-BTC`, `Điều 18`, `8471.30.20`, `A11`, `IDA`. Vector search kém với chuỗi định danh.
- Postgres `tsvector` cấu hình tiếng Việt; chuyển OpenSearch nếu đo được là chưa đủ
- Tách từ: `pyvi` hoặc `underthesea`
- Chuẩn hoá số hiệu văn bản về dạng canonical trước khi index

**Dense embedding** — đánh giá song song trên golden set, chọn theo số liệu: `BAAI/bge-m3` (mặc định đề xuất) · `Cohere embed-multilingual-v3` / `Voyage-3` · `bkai-foundation-models/vietnamese-bi-encoder`, `halong_embedding`.

**Reranker (bắt buộc)** — `BAAI/bge-reranker-v2-m3`. Top-30 từ hybrid → rerank xuống top-5..8. Bước này thường cải thiện chất lượng nhiều hơn việc đổi embedding model.

**Vector store**: `pgvector` + HNSW trên RDS. Với 200k–500k chunk là đủ, và cho phép **JOIN trực tiếp với bảng metadata hiệu lực trong cùng một truy vấn**.

### 10.3 Bộ lọc hiệu lực — lớp phòng thủ số 1

```
1. Hybrid search → top-30
2. LỌC CỨNG (trong cùng câu SQL):
     loại bỏ chunk có ngay_het_hieu_luc_hieu_dung < :ngay_tham_chieu
     hoặc            ngay_hieu_luc_hieu_dung     > :ngay_tham_chieu
3. Rerank → top-5..8
4. Gắn nhãn trạng thái hiệu lực + ngày hiệu lực vào từng chunk
```

**Truy vấn theo thời điểm.** `:ngay_tham_chieu` mặc định là hôm nay, nhưng khi phát hiện mốc thời gian trong câu hỏi (*"tờ khai tháng 3/2023 áp dụng quy định nào?"*), thay bằng ngày đó — hệ thống trở thành công cụ tra cứu pháp luật **theo thời điểm**. Tính năng các chatbot pháp lý phổ thông không có.

### 10.4 Structured lookup — dữ liệu bảng không đi qua RAG

| Bảng | Nội dung | Nguồn |
|---|---|---|
| `danh_muc_hs` | Mã HS 8 số + mô tả + ghi chú phân loại | Thông tư Danh mục hàng hóa XNK VN (AHTN) |
| `bieu_thue` | Mã HS → thuế NK ưu đãi/thông thường, VAT, TTĐB, BVMT, thuế theo FTA — **có phiên bản theo ngày hiệu lực** | Các NĐ Biểu thuế |
| `ma_loai_hinh` | Mã loại hình tờ khai + điều kiện áp dụng | Thông tư thủ tục hải quan |
| `nghiep_vu_vnaccs` | Mã nghiệp vụ + ý nghĩa, phân luồng Xanh/Vàng/Đỏ | Tài liệu VNACCS — *xem §1.2* |
| `incoterms_2020` | Ma trận 11 điều kiện × (điểm chuyển rủi ro, chi phí vận chuyển, bảo hiểm, thông quan XK, thông quan NK) | **Tự biên soạn** — §12 |

LLM truy cập qua **tool calling**, không qua vector retrieval. Loại bỏ hoàn toàn khả năng bịa thuế suất.

### 10.5 Xử lý truy vấn đầu vào

- **Từ điển viết tắt XNK** (~300 mục): `TK` → tờ khai · `C/O` → giấy chứng nhận xuất xứ · `SXXK` → sản xuất xuất khẩu · `TNTX` → tạm nhập tái xuất · `KTCN` → kiểm tra chuyên ngành
- **Cạm bẫy từ đồng nghĩa:** "thông quan" ≠ "giải phóng hàng" ≠ "đưa hàng về bảo quản" — ba khái niệm pháp lý khác nhau. **Không** gộp trong query expansion; khi phát hiện người dùng dùng lẫn, hệ thống làm rõ trong câu trả lời.
- **Nhận diện thực thể** bằng regex: số hiệu văn bản, mã HS, mã loại hình, mã nghiệp vụ, mốc thời gian → định tuyến sang structured lookup

---

## 11. Sinh câu trả lời & Guardrails

### 11.1 Model & cấu hình

- **Prototype:** `anthropic.claude-opus-5` qua Amazon Bedrock (client `AnthropicBedrockMantle`)
- **Production:** `claude-opus-5` qua Claude Platform on AWS (client `AnthropicAWS`, SigV4, model ID không tiền tố)
- **Adaptive thinking**: `thinking: {type: "adaptive"}`; `effort: "high"` cho câu hỏi nghiệp vụ, `"medium"` cho tra cứu đơn giản
- **Streaming bắt buộc** (`max_tokens` 32000 khi streaming)
- Bắt buộc kiểm tra `stop_reason` trước khi đọc `content`

### 11.2 Citations API — dùng tính năng gốc, không tự chế

Đưa chunk đã retrieve vào dưới dạng `document` content block với `citations: {enabled: true}`. Claude trả về text block kèm mảng `citations` chứa `cited_text` và vị trí ký tự chính xác trong tài liệu nguồn.

Ưu điểm so với bảo LLM tự viết `[1][2]`: trích dẫn được API bảo đảm ánh xạ về đúng đoạn văn bản gốc, **không thể bịa**. Frontend dùng dữ liệu này render chip nhảy thẳng tới đoạn tương ứng.

*Lưu ý:* Citations **không tương thích** với `output_config.format` — trả 400. Cần cả hai thì tách hai lời gọi.

### 11.3 Prompt caching

System prompt ước tính 6.000–10.000 token, **giống hệt nhau ở mọi request**.

- `cache_control: {type: "ephemeral", ttl: "1h"}` trên block cuối của system prompt
- **Thứ tự render là `tools` → `system` → `messages`.** Mọi nội dung biến động (ngày giờ, tên người dùng, tenant ID, chunk đã retrieve) phải nằm **sau** breakpoint, trong `messages`
- **Cạm bẫy chí mạng:** chèn `DateTime.Now` hoặc tenant ID vào system prompt vô hiệu hoá toàn bộ cache. Cách đúng để tiêm ngày hiện tại: mid-conversation system message (`{"role": "system", ...}` trong `messages`) — Opus 5 hỗ trợ, giữ nguyên cached prefix
- Tool definitions phải **cố định và sắp xếp tất định**
- **Giám sát:** đẩy `usage.cache_read_input_tokens` lên CloudWatch metric; alarm nếu bằng 0 nhiều request liên tiếp

### 11.4 Guardrails đặc thù XNK

Năm quy tắc **thực thi bằng code ở tầng post-processing**, không chỉ bằng câu chữ trong prompt:

**① Mã HS — rủi ro số 1.** Phân loại hàng hóa là nghiệp vụ khó nhất và mang tính pháp lý cao nhất. **Tuyệt đối không để hệ thống khẳng định một mã HS là đúng.**
- Chỉ trả ứng viên từ `danh_muc_hs` qua tool, kèm mô tả nhóm/phân nhóm và quy tắc phân loại
- Luôn kèm: *"Đây là kết quả tra cứu tham khảo. Việc phân loại hàng hóa và áp mã HS chính thức thuộc thẩm quyền của cơ quan hải quan."*
- Chủ động hướng dẫn thủ tục **xác định trước mã số hàng hóa**, đề nghị mở quy trình theo dõi (P3)

**② Thuế suất.** Chỉ lấy từ `bieu_thue` có phiên bản. Luôn kèm ngày hiệu lực và **điều kiện áp dụng ưu đãi** (thuế suất FTA chỉ áp dụng khi có C/O hợp lệ và đáp ứng quy tắc xuất xứ). Trả thuế suất ưu đãi mà không nêu điều kiện là câu trả lời sai về nghiệp vụ.

**③ Văn bản hết hiệu lực.** Nếu chunk hết hiệu lực lọt qua bộ lọc, post-processing chèn banner cảnh báo và tra đồ thị hiệu lực để trỏ tới văn bản thay thế.

**④ Công văn hướng dẫn.** Rất hữu ích trong thực tiễn nhưng **không phải văn bản QPPL** — chỉ có giá trị hướng dẫn cho vụ việc cụ thể. Bắt buộc gắn nhãn phân biệt rõ với Luật/Nghị định/Thông tư.

**⑤ Từ chối khi không đủ căn cứ.** Không chunk nào vượt ngưỡng rerank → **phải nói không biết**, đề xuất kênh khác. Cấm tuyệt đối trả lời từ kiến thức nền. Kiểm tra riêng trong bộ eval.

Cuối mỗi câu trả lời: disclaimer ngắn — nội dung tham khảo, không thay thế tư vấn pháp lý chuyên môn hoặc quyết định của cơ quan có thẩm quyền.

### 11.5 Tool definitions

```
tra_cuu_van_ban(so_hieu | tu_khoa, ngay_tham_chieu?) → nội dung + trạng thái hiệu lực
kiem_tra_hieu_luc(so_hieu, dieu?, khoan?, ngay?)     → còn/hết hiệu lực + VB thay thế
tra_cuu_ma_hs(mo_ta_hang_hoa | ma_hs)                → ứng viên HS + ghi chú phân loại
tra_cuu_thue_suat(ma_hs, nuoc_xuat_xu?, ngay?)       → các dòng thuế + điều kiện áp dụng
tra_cuu_loai_hinh(ma | mo_ta_giao_dich)              → mã loại hình + điều kiện
tra_cuu_incoterms(dieu_kien)                          → ma trận trách nhiệm (tự biên soạn)
khoi_tao_quy_trinh(loai_quy_trinh, tham_so)          → tạo process instance Camunda (P3)
```

Dùng **Tool Runner** của SDK thay vì tự viết vòng lặp agentic. `strict: true` trên tool có schema chặt.

---

## 12. Bảo mật & Multi-tenant

```
Organization (tenant)
  ├── loai: noi_bo | b2b_khach_hang | dao_tao
  ├── goi_dich_vu: quota truy vấn/tháng, tính năng
  ├── corpus_rieng[]     ← SOP nội bộ, chỉ tenant đó truy cập
  └── User → vai_tro: admin | user | viewer
```

**Cách ly dữ liệu.** Corpus pháp luật **dùng chung** (tiết kiệm chi phí index và embedding). Corpus riêng của tenant cách ly bằng `tenant_id` **áp dụng trong mệnh đề WHERE của chính truy vấn vector ở tầng database** — không lọc ở tầng application. Đây là ranh giới bảo mật, bắt buộc có test tự động chứng minh không rò rỉ chéo, và test đó là **required status check** trong CI.

**Xác thực:** nội bộ DN — SSO SAML/OIDC (production) · B2B — admin tổ chức tự cấp tài khoản · đào tạo — email + mã lớp có thời hạn. Prototype dùng JWT đơn giản cho cả ba, giữ nguyên mô hình `tenant_id`.

**Hạ tầng:** IAM task role riêng từng service · secret trong Secrets Manager (không secret nào ở biến môi trường plain-text hay trong repo) · mã hoá at-rest bằng KMS · WAF trước ALB · security group tối thiểu · service chạy trong private subnet (production).

---

## 13. Rà soát bản quyền — làm trước khi crawl

- **Văn bản QPPL Việt Nam**: không thuộc đối tượng bảo hộ quyền tác giả → được lưu trữ và sử dụng toàn văn.
- **Incoterms® 2020, UCP 600, ISBP 745, eUCP** — tài liệu ICC, **có bản quyền**. Không ingest và tái xuất bản toàn văn. Đội ngũ **tự biên soạn** phần diễn giải (ma trận trách nhiệm/chi phí/rủi ro cho 11 điều kiện: EXW, FCA, FAS, FOB, CFR, CIF, CPT, CIP, DAP, DPU, DDP).
- **WCO Harmonized System Explanatory Notes** — có bản quyền, không ingest. Chỉ dùng Danh mục hàng hóa XNK Việt Nam.
- **Trang thương mại** (thuvienphapluat, luatvietnam…): văn bản luật thì được, nhưng bình luận/tóm tắt của họ là tài sản của họ. Chỉ dùng để **đối chiếu**, ingest từ nguồn nhà nước.

---

## 14. Đánh giá chất lượng

### 14.1 Golden set

**Tài sản giá trị nhất của dự án, không thể mua được.** Cùng 2–3 chuyên gia khai báo hải quan / giảng viên XNK, thu thập câu hỏi thực tế — mỗi câu gồm: câu hỏi nguyên văn như người dùng thật sẽ hỏi (kể cả viết tắt, sai chính tả, tiếng Anh trộn tiếng Việt) · đáp án đúng do chuyên gia viết · căn cứ pháp lý · phân loại · mức độ khó.

- **Prototype:** 40–50 câu, ≥10 câu bẫy
- **Production:** 300–500 câu, ≥30 câu bẫy, tối thiểu 50 câu mỗi nhóm phân loại

Nằm trong `eval/golden-set/`, versioned trong git, mọi thay đổi cần approval của `@xnk-expert-team`.

### 14.2 Bộ chỉ số

**Retrieval** (tự động, không cần LLM — chạy trong CI):
- `Recall@10` — mục tiêu ≥ 0,90
- `MRR` sau rerank
- **`Stale Citation Rate`** — tỉ lệ kết quả chứa nội dung đã hết hiệu lực. **Mục tiêu = 0.** Chỉ số riêng của domain và quan trọng nhất; > 0 thì chặn merge.

**Generation:**
- `Citation Precision` — LLM-as-judge + human spot check 20%
- `Groundedness`
- `Correct Refusal Rate` — trên tập câu ngoài phạm vi
- **`Chuyên gia chấm điểm`** — thang 1–5. **Chỉ số quyết định go/no-go**; các chỉ số tự động chỉ là proxy.

### 14.3 Pilot (sau prototype)

50–100 người dùng thật (mỗi nhóm 1 tenant), 3–4 tuần. Rà soát toàn bộ báo cáo sai sót hàng tuần cùng chuyên gia qua quy trình P2. Chỉ mở rộng khi tỉ lệ báo lỗi ổn định dưới ngưỡng chấp nhận.

---

## 15. Chi phí

### 15.1 Theo quy mô

| Quy mô | Truy vấn/tháng | Opus 5 | Sonnet 5 | Haiku 4.5 |
|---|---|---|---|---|
| Prototype nội bộ | 500 | ~$33 | ~$20 | ~$7 |
| Pilot (50 người) | 5.000 | ~$325 | ~$195 | ~$65 |
| Production (10.000 tài khoản) | 180.000 | ~$11.700 | ~$7.020 | ~$2.340 |

> Chi phí không phải thuộc tính của công nghệ mà của quy mô. Prototype đầy đủ tính năng với model tốt nhất tốn khoảng 33 đô la một tháng.

### 15.2 Production

**Claude** (đã tính prompt caching + semantic cache): **~$6.900/tháng**

**Hạ tầng AWS:**

| Hạng mục | /tháng |
|---|---|
| ECS Fargate (7 service × 2 task) | ~$490 |
| RDS PostgreSQL Multi-AZ `db.r6g.xlarge` | ~$700 |
| RDS PostgreSQL Multi-AZ `db.t4g.medium` (Camunda) | ~$120 |
| ElastiCache Redis `cache.t4g.medium` | ~$100 |
| ALB + WAF | ~$60 |
| NAT Gateway (đã giảm nhờ VPC Endpoint) | ~$100 |
| S3 + CloudFront + ECR | ~$60 |
| CloudWatch + X-Ray | ~$100 |
| **Cộng production** | **~$1.730** |
| dev + staging (nhỏ, tắt ngoài giờ) | ~$600 |
| **Tổng hạ tầng** | **~$2.330** |

**Tổng: ~$9.200–12.200/tháng**, chưa gồm license Telerik DevCraft.

### 15.3 Đòn bẩy giảm chi phí, xếp theo tỉ lệ lợi ích/rủi ro

1. **Semantic cache (Redis)** — domain này có tỉ lệ câu hỏi lặp rất cao. Cache theo embedding câu hỏi + phiên bản corpus. Giảm 25–40% lời gọi, **không ảnh hưởng chất lượng**. Làm đầu tiên.
2. **Prompt caching** — bắt buộc, gần như miễn phí về công sức.
3. **Message Batches API** (giảm 50%) cho tác vụ nền. *Có trên Claude Platform on AWS, không có trên Bedrock — lý do chuyển sang Claude Platform ở production.*
4. **Fargate Spot** cho dev/staging — giảm ~70% phần compute không phải production.
5. **Định tuyến theo độ khó** — tra cứu định danh đơn giản trả trực tiếp từ structured lookup không qua LLM; nghiệp vụ dùng Sonnet 5; tình huống phức tạp dùng Opus 5. **Đây là đánh đổi chất lượng — chỉ áp dụng sau khi bộ eval đo được tác động thực tế trên từng nhóm câu hỏi.** Khuyến nghị mặc định: giữ Opus 5, xem lại sau pilot.

---

## 16. Lộ trình

| Phase | Nội dung | Thời lượng |
|---|---|---|
| **0.5** | **Prototype trên AWS Free Tier** — chứng minh kiến trúc, quyết định go/no-go | **7 tuần** |
| 0 | Khảo sát, chốt phạm vi, golden set đầy đủ, rà soát bản quyền, nền tảng production | 3–4 tuần |
| 1 | Data pipeline & Đồ thị hiệu lực + Camunda P1 đầy đủ | 5–7 tuần |
| 2 | Indexing & Retrieval quy mô đầy đủ | 3–4 tuần |
| 3 | Generation & Guardrails | 3–4 tuần |
| 4 | Frontend, multi-tenant SSO, Camunda P2–P4 | 4–5 tuần |
| 5 | Eval, load test & Pilot | 3–4 tuần |
| **Tổng tới v1 production** | | **~7 tháng** kể cả prototype |
| 6 | Vận hành liên tục | vĩnh viễn |

### Nhân sự (production)

- 1 Tech lead / kiến trúc sư
- 1 Data engineer — trọng tâm Phase 1, vị trí quan trọng nhất
- 2 Backend engineer (1 .NET, 1 Python)
- 1 Frontend engineer (Blazor + Telerik)
- 1 DevOps/Platform engineer
- **1–2 chuyên gia XNK, bán thời gian xuyên suốt** — **không phải nhân sự tuỳ chọn**

---

## 17. Vận hành liên tục

Không phải giai đoạn kết thúc mà là chế độ vận hành vĩnh viễn. Corpus pháp luật thay đổi liên tục; hệ thống không được cập nhật sẽ **âm thầm trở nên sai** mà không có lỗi nào phát ra.

- **Crawl gia tăng hàng tuần** (EventBridge Scheduler) → phát hiện văn bản mới → khởi tạo P1
- **Cảnh báo tự động** khi văn bản mới sửa đổi/thay thế văn bản đang có → gắn cờ cảnh báo lên chunk bị ảnh hưởng **ngay lập tức**, trước khi chuyên gia kịp review
- **Regression test** trên toàn bộ golden set sau mỗi cập nhật corpus hoặc thay đổi prompt — không kích hoạt production nếu có chỉ số suy giảm
- **Review hàng tháng** với chuyên gia: 30 câu ngẫu nhiên + toàn bộ câu bị báo sai
- **Mở rộng golden set** liên tục từ câu hỏi thực tế
- **Rà soát chi phí hàng tháng** theo tenant, phát hiện bất thường

---

## 18. Những điều KHÔNG nên làm

| Cám dỗ | Vì sao sai |
|---|---|
| **Blazor Server cho 10.000 người dùng** | Mỗi phiên giữ kết nối SignalR thường trực; chi phí tăng theo số tab đang mở. Dùng WASM. |
| **Đưa luồng hỏi–đáp qua Camunda** | Request đồng bộ dưới 10 giây, không có bước con người. BPMN chỉ thêm độ trễ và điểm hỏng. |
| **Dùng Java Delegate trong BPMN** | Khoá chặt vào Camunda 7, loại service .NET/Python khỏi cuộc chơi, chuyển sang C8 sau này gần như bất khả thi. |
| **Tách 15–20 microservice ngay từ đầu** | Chia quá nhỏ giai đoạn đầu là nguyên nhân thất bại phổ biến hơn chia quá to. |
| **AWS access key dài hạn trong GitHub Actions** | Rò rỉ một lần là mất cả tài khoản. Dùng OIDC federation. |
| **Để wizard AWS tạo NAT Gateway cho prototype** | ~$96/tháng đốt cho thứ không cần. |
| **Hạ chất lượng model ở prototype để tiết kiệm** | Chênh lệch vài chục đô la, nhưng dẫn tới kết luận sai về tính khả thi của cả dự án. |
| **Fine-tune LLM ngay từ đầu** | Không giải quyết vấn đề cốt lõi (kiến thức thay đổi liên tục), tốn kém, hallucination khó kiểm soát hơn. Chỉ cân nhắc fine-tune model **embedding** nếu eval chỉ ra retrieval là nút thắt. |
| **Fixed-size chunking (512/1024 token)** | Cắt ngang giữa Khoản, mất ngữ cảnh pháp lý, phá hỏng trích dẫn. |
| **Bỏ BM25, chỉ dùng vector** | Người dùng gõ số hiệu văn bản và mã HS — vector search kém với chuỗi định danh. |
| **Để LLM đọc bảng biểu thuế từ text chunk** | Sẽ bịa thuế suất. Bảng phải qua structured lookup. |
| **Để LLM khẳng định mã HS** | Rủi ro nghiệp vụ và pháp lý cao nhất trong toàn dự án. |
| **Chỉ lọc hiệu lực ở cấp văn bản** | Bỏ sót trường hợp phổ biến nhất: Thông tư còn hiệu lực nhưng nhiều Điều bên trong đã bị thay thế. |
| **Đưa nội dung corpus vào git** | Corpus đi qua quy trình P1, lưu trong RDS/S3. Git chỉ chứa code, prompt, BPMN, golden set. |
| **Để OCR chặn đường tới kết luận** | OCR trả lời câu "Phase 1 tốn bao nhiêu?"; retrieval trả lời câu "có nên làm không?". Đừng để câu đầu chặn câu sau. |
| **Bỏ qua chuyên gia XNK để đi nhanh** | Yếu tố quyết định thành–bại. Không có chuyên gia review dữ liệu và chấm điểm đầu ra, đội ngũ kỹ thuật **không có cách nào biết hệ thống đúng hay sai**. |
| **Ingest toàn văn Incoterms/UCP của ICC** | Vi phạm bản quyền. |
| **Coi credit AWS là mô hình kinh doanh** | $200 credit ≈ 12 giờ vận hành production. Credit là công cụ trả lời câu hỏi kiến trúc, không phải nguồn tài trợ. |
| **Coi go-live là đích đến** | Corpus không cập nhật sẽ sai dần một cách âm thầm. |

---

## 19. Kiểm chứng

**Mức 1 — Tầng dữ liệu (không cần LLM)**
```
· SQL: mọi Điều có ngay_hieu_luc; đồ thị sửa đổi không có chu trình
· Câu bẫy: hieu_luc_tai_ngay() trả về đúng trạng thái
· Đối chiếu ngẫu nhiên 50 Điều với file gốc trên S3, so sha256
```

**Mức 2 — Tầng truy xuất (không cần LLM)**
```
· Golden set → Recall@10, MRR, Stale Citation Rate
· Tenant isolation: user tenant A không bao giờ nhận chunk tenant B   ← required check
· Truy vấn theo thời điểm: hỏi "quy định tháng 3/2023" trả đúng VB thời điểm đó
```

**Mức 3 — Đầu-cuối (có LLM)**
```
· Golden set qua full pipeline
· LLM-as-judge: citation precision, groundedness
· Hành vi từ chối: câu ngoài phạm vi phải từ chối đúng cách
· Guardrail HS: mọi câu hỏi về mã HS phải kèm cảnh báo thẩm quyền
· Prompt cache: usage.cache_read_input_tokens > 0 từ request thứ hai
· Chuyên gia XNK chấm ngẫu nhiên, thang 1–5
```

**Quy trình Camunda**
```
· Unit test từng External Task Worker
· Test quy trình đầu-cuối bằng camunda-bpm-assert (engine in-memory)
· Mọi Service Task trong .bpmn đều là External Task   ← required check (ci-bpmn)
· Test timer escalation bằng cách tua nhanh đồng hồ engine
```

**Load test trước go-live**
```
· 150 concurrent request
· p95 latency < 8s (streaming first-token < 2s)
· Không lỗi rate limit ở tải đỉnh
· Xác nhận blue/green tự rollback khi CloudWatch alarm kích hoạt
```
