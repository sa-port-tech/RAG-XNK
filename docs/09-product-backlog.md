# Product Backlog — Prototype

> **Trạng thái: 🔶 Bản nháp BA — story chạm nghiệp vụ cần chuyên gia XNK xác nhận AC (điều kiện Definition of Ready).**
> Sở hữu: BA (chi tiết hoá) · Duyệt & ưu tiên: PO · Nơi sống thật: GitHub Projects

---

## Quy ước

**Định dạng story:** `Là <vai trò>, tôi muốn <hành động>, để <giá trị>` + AC dạng Given/When/Then kiểm chứng được bằng máy hoặc bằng chuyên gia.

| Ký hiệu | Nghĩa |
|---|---|
| **P0** | Sprint Goal thất bại nếu không có |
| P1 | Should-have, làm nếu còn thời gian |
| P2 | Nice-to-have, thường trôi sang Phase 1 |
| 🔶 | **Cần chuyên gia XNK xác nhận AC trước khi vào sprint** |
| ⚙️ | Có test tự động là required status check trong CI |

**Điểm:** Fibonacci (1, 2, 3, 5, 8, 13). Velocity **không dùng làm KPI** — xem [`01`](01-ke-hoach-prototype-scrum.md) §3.1.

**Tổng quan:** 8 epic · 63 story · ~430 điểm.

---

# E1 — Nền tảng hạ tầng & CI/CD

**Sprint 0** · Sprint Goal: *"Đội ngũ merge được một PR và thấy nó tự động chạy trên môi trường dev AWS."*

| ID | Story | Ưu tiên | Điểm |
|---|---|---|---|
| E1-01 | Tạo AWS account, bật MFA, cấu hình AWS Organizations | P0 | 3 |
| E1-02 | **Kiểm chứng điều khoản credit AWS** — có loại trừ Marketplace không | **P0** | 2 |
| E1-03 | Bật quyền truy cập model trên Amazon Bedrock, gọi thử `anthropic.claude-opus-5` | P0 | 2 |
| E1-04 | CDK bootstrap + VPC (public subnet, **không NAT Gateway**) | P0 | 5 |
| E1-05 | RDS PostgreSQL + pgvector, RDS riêng cho Camunda | P0 | 5 |
| E1-06 | ECS Fargate Spot cluster + ALB + Service Connect | P0 | 8 |
| E1-07 | **OIDC federation GitHub → AWS**, không dùng access key | **P0** | 5 |
| E1-08 | GitHub Project board + Issue Forms + CODEOWNERS + rulesets | P0 | 3 |
| E1-09 | 7 workflow CI với path filter | P0 | 8 |
| E1-10 | `cd-deploy.yml` — build image → ECR → deploy dev → smoke test | P0 | 8 |
| E1-11 | Skeleton 7 service, mỗi service có healthcheck | P0 | 8 |
| E1-12 | Camunda 7 Community Edition trên Fargate + Cockpit sau ALB nội bộ | P0 | 5 |
| E1-13 | **Budget alarm + tag + lịch tắt tài nguyên ngoài giờ** | **P0** | 5 |
| E1-14 | Jaeger container + OpenTelemetry trong 7 service | P1 | 5 |
| E1-15 | `ci-bpmn.yml` — **chặn Java Delegate lọt vào file `.bpmn`** ⚙️ | P1 | 3 |

### E1-02 — Kiểm chứng credit AWS · P0 · 2 điểm

> Là **Product Owner**, tôi cần biết chắc credit AWS có trả cho chi phí gọi model không, để lập ngân sách đúng.

**AC**
- Given tài khoản AWS mới với credit, When gọi Bedrock và chờ hoá đơn cập nhật, Then xác định được credit **có** hay **không** trừ vào khoản Bedrock
- Đọc và ghi lại điều khoản credit, đặc biệt mục loại trừ AWS Marketplace
- Kết quả ghi vào `docs/01` §7 và báo cáo PO **trong 2 ngày đầu Sprint 0**
- Nếu credit không áp dụng → kích hoạt phương án B (1 × EC2 `t4g.large`)

### E1-07 — OIDC federation · P0 · 5 điểm

> Là **DevOps**, tôi muốn GitHub Actions truy cập AWS bằng token ngắn hạn, để không có access key dài hạn nào tồn tại.

**AC**
- Không có `AWS_ACCESS_KEY_ID` nào trong GitHub Secrets — verify bằng cách liệt kê secrets
- IAM role có trust policy giới hạn theo `repo:<org>/<repo>:ref:refs/heads/main`
- Workflow deploy chạy thành công mà không có credential tĩnh
- Thử assume role từ nhánh khác `main` → bị từ chối

### E1-13 — Kiểm soát chi phí · P0 · 5 điểm

> Là **Product Owner**, tôi muốn được cảnh báo trước khi credit cạn, để không mất môi trường giữa sprint.

**AC**
- AWS Budgets gửi email ở mốc $50 / $100 / $150
- Cost Anomaly Detection đã bật
- Mọi tài nguyên do CDK tạo đều có tag `Project=rag-xnk` và `Env=dev`
- EventBridge Scheduler tắt Fargate task (desired-count 0) và stop RDS lúc 19h, khởi động lại 7h các ngày làm việc
- **Xác nhận không có NAT Gateway nào tồn tại trong VPC**
- Cost Explorer lọc theo tag cho ra được chi phí dự án

---

# E2 — Corpus & Đồ thị hiệu lực

**Sprint 1** · Sprint Goal: *"Chứng minh bằng truy vấn SQL rằng hệ thống phân biệt đúng điều khoản còn/hết hiệu lực trên cặp Thông tư sửa đổi."*

| ID | Story | Ưu tiên | Điểm |
|---|---|---|---|
| E2-01 | 🔶 Schema `van_ban` / `dieu_khoan` / `sua_doi` + migration | P0 | 8 |
| E2-02 | 🔶 Hàm `hieu_luc_tai_ngay(dieu_khoan_id, ngay)` ⚙️ | **P0** | 8 |
| E2-03 | Tải 15 văn bản, lưu S3 kèm `sha256`, ghi metadata | P0 | 5 |
| E2-04 | Trích xuất text từ PDF text layer / DOCX | P0 | 5 |
| E2-05 | 🔶 Structural parser — phân rã Phần/Chương/Mục/Điều/Khoản/Điểm | **P0** | 13 |
| E2-06 | Kiểm tra toàn vẹn cấu trúc, đẩy trường hợp nghi ngờ vào hàng đợi review | P0 | 5 |
| E2-07 | 🔶 Nạp metadata hiệu lực từ bảng chuyên gia điền ([`04`](04-danh-muc-van-ban-loi.md) §3) | P0 | 5 |
| E2-08 | 🔶 Nạp quan hệ sửa đổi cấp Điều/Khoản ([`04`](04-danh-muc-van-ban-loi.md) §4) | **P0** | 8 |
| E2-09 | Trích quan hệ sửa đổi bằng LLM để đối chiếu với bản chuyên gia | P1 | 8 |
| E2-10 | 🔶 Ưu tiên VBHN khi có (BR-09) | P1 | 3 |
| E2-11 | Kiểm tra đồ thị sửa đổi không có chu trình ⚙️ | P0 | 3 |
| E2-12 | Màn hình review cấu trúc cho chuyên gia (tối giản) | P1 | 8 |

### E2-02 — Truy vấn hiệu lực theo thời điểm 🔶⚙️ · P0 · 8 điểm

> Là **chuyên gia nghiệp vụ**, tôi muốn hệ thống biết một Điều đã bị Thông tư khác sửa đổi, để không bị dẫn chiếu tới nội dung lỗi thời.

**AC**
- Given ngày tham chiếu là hôm nay, When gọi `hieu_luc_tai_ngay(<điều đã bị sửa>)`, Then trả về `false`
- Given ngày tham chiếu trước ngày hiệu lực của văn bản sửa đổi, When gọi hàm đó, Then trả về `true`
- Given một Điều chưa bao giờ bị sửa, When gọi hàm với bất kỳ ngày nào sau ngày hiệu lực, Then trả về `true`
- Bảng `sua_doi` ghi nhận đúng quan hệ với `pham_vi_anh_huong` liệt kê **từng Điều cụ thể**, không phải mô tả chung
- **Toàn bộ 10 câu bẫy trả lời đúng bằng SQL thuần, chưa cần LLM**
- Chuyên gia XNK xác nhận danh sách Điều bị tác động là đúng

> Đây là story quan trọng nhất của toàn bộ prototype. Nếu nó không đạt, Sprint 2 và 3 không có nền để đứng.

### E2-05 — Structural parser 🔶 · P0 · 13 điểm

> Là **hệ thống truy xuất**, tôi cần mỗi Khoản/Điểm là một bản ghi có đường dẫn cấu trúc đầy đủ, để trích dẫn chính xác tới cấp Điều/Khoản/Điểm.

**AC**
- 15 văn bản được phân rã thành bản ghi `dieu_khoan` có `duong_dan_cau_truc` đầy đủ dạng `<số hiệu> > Chương X > Mục Y > Điều Z > Khoản n > Điểm m`
- Regex xử lý được các mẫu: `Điều N.` · `N.` · `a)` · `Chương [số La Mã]` · `Mục N`
- Trường hợp regex không khớp → gắn cờ `can_review`, không tự đoán
- Số Điều liên tục; phát hiện được chỗ nhảy cóc
- **Chuyên gia review ngẫu nhiên 10% → độ chính xác cấu trúc ≥ 98%**
- Bảng biểu trong văn bản **không** bị đưa vào `dieu_khoan` — tách sang xử lý riêng

---

# E3 — Truy xuất

**Sprint 2** · Sprint Goal (phần 1): *"Không bao giờ trả về văn bản đã hết hiệu lực."*

| ID | Story | Ưu tiên | Điểm |
|---|---|---|---|
| E3-01 | Chunking theo Điều, kèm header ngữ cảnh | P0 | 5 |
| E3-02 | Sinh embedding BGE-M3 (ONNX, CPU) + lưu pgvector HNSW | P0 | 8 |
| E3-03 | BM25 / full-text tiếng Việt trên Postgres `tsvector` | P0 | 8 |
| E3-04 | Hybrid search — hợp nhất kết quả BM25 + vector | P0 | 5 |
| E3-05 | **Bộ lọc hiệu lực trong câu SQL** ⚙️ | **P0** | 8 |
| E3-06 | Reranker BGE-reranker-v2-m3, top-30 → top-8 | P0 | 5 |
| E3-07 | Nhận diện thực thể: số hiệu VB, mã HS, mã loại hình, mốc thời gian | P0 | 5 |
| E3-08 | **Truy vấn theo thời điểm** — mốc thời gian trong câu hỏi ⚙️ | P0 | 5 |
| E3-09 | 🔶 Chuẩn hoá số hiệu văn bản về dạng canonical | P0 | 3 |
| E3-10 | 🔶 Query expansion bằng từ điển viết tắt ([`11`](11-tu-dien-thuat-ngu-xnk.md)) | P1 | 5 |
| E3-11 | Đánh giá song song 2–3 model embedding trên golden set | P1 | 8 |
| E3-12 | Đo Recall@10, MRR, Stale Citation Rate trong CI ⚙️ | P0 | 5 |

### E3-05 — Bộ lọc hiệu lực 🔶⚙️ · P0 · 8 điểm

> Là **khai báo viên**, tôi muốn kết quả tra cứu không bao giờ chứa điều khoản đã hết hiệu lực, để không nộp tờ khai theo quy định cũ.

**AC**
- `Stale Citation Rate = 0` trên bộ 10 câu bẫy
- Bộ lọc thực thi **trong câu SQL cùng với vector search**, không ở tầng application — verify bằng code review và bằng cách đọc query log
- Mỗi chunk trả về kèm `trang_thai_hieu_luc` và `ngay_hieu_luc`
- Given corpus có chunk hết hiệu lực, When truy vấn từ khoá khớp chunk đó, Then chunk không xuất hiện trong kết quả
- Test này là **required status check** — `Stale Citation Rate > 0` chặn merge

### E3-08 — Truy vấn theo thời điểm ⚙️ · P0 · 5 điểm

> Là **khai báo viên xử lý tờ khai cũ**, tôi muốn tra được quy định áp dụng tại thời điểm mở tờ khai, để giải trình với cơ quan hải quan.

**AC**
- Given câu hỏi chứa *"tháng 3/2023"*, When truy vấn, Then `:ngay_tham_chieu` = `2023-03-31` (hoặc mốc phù hợp) chứ không phải hôm nay
- Given câu hỏi không có mốc thời gian, Then `:ngay_tham_chieu` = hôm nay
- Nhận diện được các dạng: *"tháng M/YYYY"* · *"năm YYYY"* · *"ngày DD/MM/YYYY"* · *"trước khi TT XX có hiệu lực"*
- Câu trả lời nêu rõ ngày tham chiếu đang dùng

### E3-04 — Hybrid search · P0 · 5 điểm

**AC**
- Given câu hỏi chứa số hiệu văn bản chính xác, When truy vấn, Then văn bản đó nằm trong top-3 *(BM25 phải thắng ở ca này)*
- Given câu hỏi diễn đạt tự nhiên không chứa từ khoá chính xác, When truy vấn, Then Recall@10 ≥ 0,90 *(vector phải thắng ở ca này)*
- Trọng số hợp nhất có thể cấu hình và được ghi trong ADR

---

# E4 — Sinh câu trả lời & Guardrails

**Sprint 2** · Sprint Goal (phần 2): *"Trả lời câu hỏi nghiệp vụ thật kèm trích dẫn chính xác."*

| ID | Story | Ưu tiên | Điểm |
|---|---|---|---|
| E4-01 | Tích hợp Bedrock + streaming + xử lý `stop_reason` | P0 | 8 |
| E4-02 | **Citations API** — chunk dạng `document` block ⚙️ | **P0** | 8 |
| E4-03 | 🔶 **Guardrail ① — mã HS** ⚙️ | **P0** | 5 |
| E4-04 | 🔶 Guardrail ② — thuế suất kèm điều kiện ưu đãi ⚙️ | P0 | 5 |
| E4-05 | **Guardrail ⑤ — từ chối khi không đủ căn cứ** ⚙️ | **P0** | 5 |
| E4-06 | 🔶 Guardrail ④ — gắn nhãn công văn hướng dẫn ⚙️ | P0 | 3 |
| E4-07 | Guardrail ③ — cảnh báo văn bản hết hiệu lực + trỏ VB thay thế | P0 | 5 |
| E4-08 | Disclaimer cuối câu trả lời (BR-10) ⚙️ | P0 | 2 |
| E4-09 | Prompt caching + giám sát `cache_read_input_tokens` ⚙️ | P0 | 5 |
| E4-10 | Tool `tra_cuu_van_ban`, `kiem_tra_hieu_luc` | P0 | 8 |
| E4-11 | Tool `tra_cuu_ma_hs`, `tra_cuu_thue_suat`, `tra_cuu_loai_hinh` | P1 | 8 |
| E4-12 | 🔶 System prompt v1 + từ điển thuật ngữ | P0 | 8 |
| E4-13 | Guardrail chi tiêu: đếm token, hạn mức ngày, log `usage` | P0 | 5 |
| E4-14 | 🔶 Xử lý BR-08 — ba khái niệm thông quan/giải phóng/đưa về bảo quản | P1 | 5 |

### E4-02 — Citations API ⚙️ · P0 · 8 điểm

> Là **khai báo viên**, tôi muốn mỗi khẳng định có trích dẫn dẫn thẳng tới đoạn văn bản gốc, để tự kiểm chứng trước khi áp dụng.

**AC**
- Chunk được truyền vào dưới dạng `document` content block với `citations: {enabled: true}`
- Response chứa mảng `citations` có `cited_text` và vị trí ký tự
- Given một câu trả lời có 3 khẳng định nghiệp vụ, Then có ít nhất 3 citation ánh xạ về đúng chunk
- **Không dùng cách bảo LLM tự viết `[1][2]` trong prompt** — verify bằng code review
- API trả 400 khi kết hợp với `output_config.format` → tách hai lời gọi nếu cần

### E4-03 — Guardrail mã HS 🔶⚙️ · P0 · 5 điểm

> Là **người dùng hỏi về mã HS**, tôi muốn được cảnh báo rõ đây chỉ là tham khảo, để không tự áp mã và bị bác tờ khai.

**AC**
- Given câu trả lời có chứa chuỗi khớp mẫu mã HS (`\d{4}\.\d{2}` hoặc `\d{8}`), Then câu trả lời **bắt buộc** chứa cảnh báo thẩm quyền — kiểm tra ở tầng post-processing bằng code, không phụ thuộc prompt
- Cảnh báo nêu: phân loại chính thức thuộc thẩm quyền cơ quan hải quan
- Câu trả lời chủ động nêu thủ tục **xác định trước mã số hàng hóa**
- Không câu nào dùng ngôn ngữ khẳng định tuyệt đối (*"mã HS là…"*) mà thiếu cảnh báo
- Test tự động chạy trên toàn bộ golden set nhóm K → 100% pass, là required check

### E4-05 — Từ chối khi không đủ căn cứ ⚙️ · P0 · 5 điểm

> Là **học viên**, tôi muốn hệ thống nói thẳng là không biết thay vì bịa, để tôi biết khi nào phải hỏi người thật.

**AC**
- Given 20 câu hỏi ngoài phạm vi ([`03`](03-pham-vi-nghiep-vu-v1.md) §5), When hỏi, Then ≥ 18 câu bị từ chối đúng cách → `Correct Refusal Rate ≥ 0,90`
- Given không chunk nào vượt ngưỡng rerank, Then hệ thống từ chối, **không** sinh câu trả lời từ kiến thức nền
- Câu từ chối đề xuất kênh thay thế (hotline hải quan, chuyên gia nội bộ, thủ tục hỏi đáp chính thức)
- LLM-as-judge + human spot check 20% xác nhận không có nội dung bịa

---

# E5 — Giao diện & Multi-tenant

**Sprint 3** · Sprint Goal: *"Chuyên gia XNK tự dùng hệ thống qua giao diện thật."*

| ID | Story | Ưu tiên | Điểm |
|---|---|---|---|
| E5-01 | Blazor WASM + Telerik, khung ứng dụng, đăng nhập JWT | P0 | 8 |
| E5-02 | **Chat streaming SSE** (spike đã làm ở Sprint 1) | P0 | 8 |
| E5-03 | Panel văn bản gốc + chip trích dẫn bấm được, highlight đúng đoạn | P0 | 8 |
| E5-04 | **Cách ly `tenant_id` ở tầng database** ⚙️ | **P0** | 5 |
| E5-05 | Badge trạng thái hiệu lực (xanh/vàng/đỏ) cạnh mỗi trích dẫn | P0 | 3 |
| E5-06 | 3 loại tenant + RBAC (admin/user/viewer) | P0 | 8 |
| E5-07 | Nút "Báo cáo sai sót" → ghi nhận vào hàng đợi | P0 | 3 |
| E5-08 | Lịch sử hội thoại | P1 | 5 |
| E5-09 | Màn hình hàng đợi review cho chuyên gia (Telerik Grid) | P1 | 8 |
| E5-10 | Màn hình quản lý corpus | P1 | 8 |
| E5-11 | Dashboard chi phí & chỉ số chất lượng | P2 | 5 |
| E5-12 | Responsive cho điện thoại | P1 | 5 |
| E5-13 | Rate limit theo tenant | P1 | 3 |

### E5-04 — Cách ly dữ liệu giữa tenant ⚙️ · P0 · 5 điểm

> Là **khách hàng B2B**, tôi cần chắc chắn tài liệu nội bộ của tôi không bao giờ xuất hiện trong câu trả lời cho khách hàng khác.

**AC**
- Given tenant A có corpus riêng và tenant B không có quyền, When user của B truy vấn từ khoá khớp tài liệu của A, Then không chunk nào của A xuất hiện
- Bộ lọc `tenant_id` nằm **trong mệnh đề WHERE của truy vấn vector**, không ở tầng application
- Test tự động sinh dữ liệu 2 tenant và chạy chéo 50 truy vấn → 0 rò rỉ
- **Required status check** — không pass thì không merge
- CISO (S10) xác nhận mô hình cách ly

### E5-02 — Chat streaming SSE · P0 · 8 điểm

> Là **người dùng**, tôi muốn thấy câu trả lời hiện dần, để biết hệ thống đang xử lý chứ không bị treo.

**AC**
- Token đầu tiên xuất hiện < 2s
- `HttpClient` đọc response stream, đẩy vào `IAsyncEnumerable<string>`
- `StateHasChanged()` có throttle ~50ms, không render mỗi token
- Ngắt mạng giữa chừng → hiện thông báo lỗi rõ ràng, không treo màn hình
- Hoạt động trên Chrome, Edge, Safari và trên trình duyệt di động

---

# E6 — Quy trình Camunda P1

**Sprint 3** · Chi tiết đặc tả: [`10`](10-dac-ta-quy-trinh-bpmn.md)

| ID | Story | Ưu tiên | Điểm |
|---|---|---|---|
| E6-01 | 🔶 File `.bpmn` quy trình P1 rút gọn, deploy qua CI | P0 | 8 |
| E6-02 | External Task Worker (Python) — trích xuất, phân rã, metadata | P0 | 8 |
| E6-03 | External Task Worker (.NET) — gắn cờ cảnh báo, thông báo | P0 | 5 |
| E6-04 | User Task: chuyên gia review phạm vi ảnh hưởng, render bằng Telerik | P0 | 8 |
| E6-05 | User Task: chuyên gia phê duyệt đưa vào corpus | P0 | 5 |
| E6-06 | Timer boundary event + escalation sau 5 ngày làm việc | P1 | 5 |
| E6-07 | Test quy trình đầu-cuối bằng `camunda-bpm-assert` ⚙️ | P1 | 5 |

### E6-01 — Quy trình P1 rút gọn 🔶 · P0 · 8 điểm

**AC**
- Mọi Service Task trong file `.bpmn` đều là **External Task** — `ci-bpmn.yml` chặn Java Delegate ⚙️
- Given một văn bản mới được nạp, When khởi tạo process instance, Then quy trình chạy qua các bước và dừng ở User Task đầu tiên
- Chuyên gia hoàn thành User Task → quy trình tiếp tục
- File `.bpmn` nằm trong `bpmn/`, deploy lên engine qua pipeline, **không sửa trực tiếp trên Modeler production**
- Chuyên gia XNK xác nhận luồng nghiệp vụ đúng thực tế

---

# E7 — Đánh giá chất lượng

**Xuyên suốt** · Phương pháp: [`14`](14-phuong-phap-golden-set.md)

| ID | Story | Ưu tiên | Điểm |
|---|---|---|---|
| E7-01 | 🔶 Golden set vòng 1 — 20 câu gồm 5 câu bẫy | P0 | 8 |
| E7-02 | 🔶 Golden set đầy đủ — 40–50 câu gồm ≥10 câu bẫy | P0 | 13 |
| E7-03 | Bộ đo retrieval: Recall@10, MRR, Stale Citation Rate ⚙️ | P0 | 8 |
| E7-04 | LLM-as-judge: Citation Precision, Groundedness | P0 | 8 |
| E7-05 | `eval-regression.yml` — required check khi đụng prompts/retrieval/generation ⚙️ | **P0** | 8 |
| E7-06 | Bot comment bảng so sánh trước/sau vào PR | P1 | 5 |
| E7-07 | 🔶 Phiếu chấm điểm chuyên gia thang 1–5 | P0 | 3 |
| E7-08 | Báo cáo eval cuối mỗi sprint | P0 | 3 |

### E7-05 — Cổng chất lượng trong CI ⚙️ · P0 · 8 điểm

> Là **Product Owner**, tôi muốn không ai merge được thay đổi làm hệ thống trích dẫn văn bản hết hiệu lực, kể cả vô tình.

**AC**
- Workflow kích hoạt khi PR đụng `prompts/`, `retrieval/`, `generation/`, `eval/`
- Chạy toàn bộ golden set, tính đủ 5 chỉ số
- **`Stale Citation Rate > 0` → chặn merge tuyệt đối**
- `Recall@10 < 0,90` → chặn merge
- Bất kỳ chỉ số nào tụt > 2% so với baseline trên `main` → chặn merge
- Kết quả comment vào PR dạng bảng so sánh
- Baseline lưu trong `eval/golden-set/baseline.json`, cập nhật khi merge vào `main`

---

# E8 — Spike

| ID | Story | Ưu tiên | Điểm | Timebox |
|---|---|---|---|---|
| E8-01 | **Spike: streaming SSE trên Blazor WASM** | P0 | 5 | 2 ngày, Sprint 1 |
| E8-02 | **Spike: đo chất lượng OCR** | P2 | 5 | 3 ngày, Sprint 1 |
| E8-03 | Spike: so sánh model embedding tiếng Việt | P1 | 5 | 3 ngày, Sprint 2 |

### E8-01 — Spike streaming Blazor WASM · P0 · 5 điểm · timebox 2 ngày

> Là **Tech Lead**, tôi cần biết sớm streaming SSE trên Blazor WASM có vướng gì không, để không phát hiện ở Sprint 3.

**AC**
- Prototype tối giản: 1 endpoint SSE + 1 component Blazor hiện text dần
- Chạy được trên Chrome, Edge, Safari và trình duyệt di động
- Đo được độ trễ token đầu tiên
- Báo cáo 1 trang: có vướng gì, cần giải pháp gì
- **Hết 2 ngày là dừng và báo cáo**, kể cả chưa xong

### E8-02 — Spike OCR · P2 · 5 điểm · timebox 3 ngày

> Là **Product Owner**, tôi cần số liệu về OCR để ước lượng công sức Phase 1.

**AC**
- 3–5 văn bản scan tiêu biểu, ưu tiên phụ lục biểu mẫu và danh mục hàng hóa
- Chạy qua VietOCR, PaddleOCR-vi, Amazon Textract
- Báo cáo có **hai** con số: độ chính xác ký tự **và thời gian chuyên gia phải sửa lại**
- **Kết quả không nối vào pipeline chính**
- Hết 3 ngày là dừng và báo cáo

---

## Phân bổ theo Sprint

| Sprint | Epic | Story | Điểm | Must-have |
|---|---|---|---|---|
| 0 | E1 | 15 | 75 | E1-02, E1-07, E1-13 |
| 1 | E2, E7 (một phần), E8-01, E8-02 | 22 | 145 | **E2-02**, E2-05, E2-08 |
| 2 | E3, E4, E7 | 26 | 165 | **E3-05**, E4-02, E4-03, E4-05, E7-05 |
| 3 | E5, E6 | 20 | 130 | **E5-04**, E5-02, E6-01 |

Con số điểm chỉ để so sánh tương đối giữa story. **PO cam kết với stakeholder về Sprint Goal, không cam kết về danh sách story** — xem [`01`](01-ke-hoach-prototype-scrum.md) §3.1.

---

## Ngoài phạm vi — đưa vào Backlog Phase 1

Danh sách này PO công bố ở Sprint 0 và **không thương lượng trong sprint**. Đề xuất phát sinh giữa sprint đi vào đây, không chen vào sprint đang chạy.

SSO SAML/OIDC · Crawler tự động · Camunda P2/P3/P4 · Semantic cache Redis · Batches API · Multi-AZ / HA · Blue-green deployment · Môi trường staging & production · Mobile app · Xuất báo cáo · Tích hợp hệ thống khai báo hải quan bên ngoài · Đa ngôn ngữ · Quản lý quota & billing · Corpus riêng cho tenant B2B *(chờ trả lời Q2 trong [`03`](03-pham-vi-nghiep-vu-v1.md) §7)*.
