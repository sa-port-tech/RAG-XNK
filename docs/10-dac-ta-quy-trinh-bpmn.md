# Đặc tả quy trình BPMN

> **Trạng thái: 🔶 Bản nháp BA — luồng nghiệp vụ cần chuyên gia XNK xác nhận đúng thực tế.**
> Sở hữu: BA · Duyệt: PO + Chuyên gia XNK + Tech Lead · File `.bpmn` sống tại `bpmn/`

---

## 0. Quy ước bắt buộc

**Mọi Service Task phải là External Task.** Không Java Delegate, không script nhúng trong BPMN. Đây vừa là điều kiện kỹ thuật để service .NET/Python tham gia được (engine Camunda 7 là Java), vừa là biện pháp giảm rủi ro khi Camunda 7 kết thúc hỗ trợ. `ci-bpmn.yml` chặn vi phạm ở tầng CI.

| Ký hiệu trong tài liệu | BPMN element |
|---|---|
| `[S]` | Service Task (External Task) |
| `[U]` | User Task |
| `<G>` | Exclusive Gateway |
| `(T)` | Timer Boundary Event |
| `(E)` | Error Boundary Event |

**Đặt tên topic External Task:** `<service>.<hành động>` — ví dụ `ingestion.extract-text`, `corpus.flag-affected-chunks`.

---

## 1. P1 — Đưa văn bản mới vào corpus

**Ưu tiên: P0 · Sprint 3 (bản rút gọn) · Đây là quy trình duy nhất làm ở prototype.**

### 1.1 Vì sao quy trình này xứng đáng dùng Camunda

| Đặc điểm | Có ở P1? |
|---|---|
| Chạy dài (ngày tới tuần) | ✅ |
| Có bước con người bắt buộc | ✅ 2 User Task |
| Cần trạng thái bền vững qua nhiều phiên | ✅ |
| Có timer và escalation | ✅ |
| Có rẽ nhánh theo điều kiện nghiệp vụ | ✅ 3 gateway |
| Cần audit trail | ✅ — ai duyệt, lúc nào |

Đối chiếu: luồng hỏi–đáp thường **không** có đặc điểm nào trong bảng này, nên không đưa qua Camunda.

### 1.2 Luồng đầy đủ (production)

```
(Start) ← EventBridge phát hiện văn bản mới
   │
   ├─[S] ingestion.download-and-store      → tải file, lưu S3, tính sha256
   ├─[S] ingestion.extract-text            → PDF text layer / OCR / DOCX
   ├─[S] ingestion.parse-structure         → phân rã Điều/Khoản/Điểm
   │
   <G1> Độ tin cậy parser ≥ 98%?
   │   ├─ Không ─[U] Chuyên gia sửa cấu trúc thủ công    (T: 5 ngày → escalate)
   │   └─ Có ────┐
   │
   ├─[S] ingestion.extract-metadata        → ngày hiệu lực, cơ quan, trạng thái
   ├─[S] ingestion.detect-amendments       → dò quan hệ sửa đổi/thay thế
   │
   <G2> Có sửa đổi văn bản đang có trong corpus?
   │   └─ Có ─[S] corpus.flag-affected-chunks   → gắn cờ cảnh báo NGAY
   │          └─[U] ★ Chuyên gia review phạm vi ảnh hưởng  (T: 5 ngày → escalate)
   │
   ├─[U] Chuyên gia phê duyệt đưa vào corpus     (T: 5 ngày → escalate)
   ├─[S] retrieval.embed-and-index
   ├─[S] generation.run-regression          → chạy golden set
   │
   <G3> Có chỉ số suy giảm?
   │   ├─ Có ─[U] Điều tra ─[S] corpus.rollback-index → (End: thất bại)
   │   └─ Không ─[S] corpus.activate → (End: thành công)
```

### 1.3 ★ Bước không được bỏ qua

**`[U] Chuyên gia review phạm vi ảnh hưởng`** là cổng chất lượng quan trọng nhất của toàn hệ thống.

Khi một văn bản mới sửa đổi văn bản đang có trong corpus, hệ thống phải biết **chính xác Điều/Khoản nào bị tác động**. LLM trích xuất được danh sách sơ bộ, nhưng sai ở đây là **sai vĩnh viễn mọi câu trả lời sau đó** — vì đồ thị hiệu lực là nền của bộ lọc hiệu lực.

Vì vậy:
- Không có nhánh "tự động duyệt" cho bước này, kể cả khi LLM tự tin
- `corpus.flag-affected-chunks` chạy **trước** khi chuyên gia review — thà cảnh báo thừa còn hơn để người dùng nhận nội dung sai trong lúc chờ
- Nếu quá 5 ngày làm việc → escalate lên trưởng nhóm chuyên môn; quá 10 ngày → thông báo PO

### 1.4 Bảng External Task

| Topic | Service | Ngôn ngữ | Input | Output | Retry |
|---|---|---|---|---|---|
| `ingestion.download-and-store` | ingestion | Python | `nguon_url` | `s3_key`, `sha256` | 3, backoff 30s |
| `ingestion.extract-text` | ingestion | Python | `s3_key` | `text_raw`, `phuong_phap` (text-layer/ocr) | 2 |
| `ingestion.parse-structure` | ingestion | Python | `text_raw` | `dieu_khoan[]`, `do_tin_cay` | 2 |
| `ingestion.extract-metadata` | ingestion | Python | `text_raw`, `nguon_url` | metadata `van_ban` | 2 |
| `ingestion.detect-amendments` | ingestion | Python | `van_ban_id`, `text_raw` | `sua_doi[]` (đề xuất) | 2 |
| `corpus.flag-affected-chunks` | corpus | .NET | `sua_doi[]` | số chunk bị gắn cờ | 3 |
| `corpus.notify-expert` | corpus | .NET | `user_task_id`, người nhận | — | 3 |
| `retrieval.embed-and-index` | retrieval | Python | `van_ban_id` | số chunk đã index | 2 |
| `generation.run-regression` | generation | Python | — | bảng chỉ số | 1 |
| `corpus.activate` | corpus | .NET | `van_ban_id` | — | 3 |
| `corpus.rollback-index` | corpus | .NET | `van_ban_id` | — | 3 |

**Xử lý lỗi:** hết retry → `bpmnError` với mã lỗi → Error Boundary Event → thông báo DevOps và dừng instance ở trạng thái chờ can thiệp. **Không tự động bỏ qua bước lỗi.**

### 1.5 Biến process

| Biến | Kiểu | Ghi ở bước |
|---|---|---|
| `van_ban_id` | String | Start |
| `nguon_url` | String | Start |
| `s3_key`, `sha256` | String | download-and-store |
| `do_tin_cay_parser` | Double | parse-structure |
| `co_sua_doi_corpus` | Boolean | detect-amendments |
| `so_chunk_bi_anh_huong` | Integer | flag-affected-chunks |
| `nguoi_duyet` | String | User Task |
| `ket_qua_regression` | Json | run-regression |
| `chi_so_suy_giam` | Boolean | run-regression |

### 1.6 Bản rút gọn cho prototype (E6-01)

Bỏ để vừa Sprint 3, giữ nguyên xương sống:

| Giữ | Bỏ |
|---|---|
| download-and-store, extract-text, parse-structure | Nhánh OCR (dùng văn bản text layer) |
| G1 + User Task sửa cấu trúc | — |
| extract-metadata, detect-amendments | — |
| G2 + flag-affected-chunks + **★ User Task review** | — |
| User Task phê duyệt | — |
| embed-and-index | — |
| — | run-regression, G3, rollback *(chạy thủ công ở prototype)* |
| Timer 5 ngày trên User Task | Escalation nhiều cấp |

### 1.7 Câu hỏi cho chuyên gia 🔶

| # | Câu hỏi |
|---|---|
| 1 | Ngưỡng 98% độ tin cậy parser có hợp lý không, hay nên cao/thấp hơn? |
| 2 | 5 ngày làm việc cho mỗi User Task có khớp nhịp làm việc thực tế không? |
| 3 | Ai là người escalate lên khi quá hạn? |
| 4 | Có cần thêm bước "đối chứng giữa hai chuyên gia" khi hai người không đồng ý không? |
| 5 | Khi chuyên gia bác bỏ văn bản (không đưa vào corpus), quy trình xử lý thế nào? |

---

## 2. P2 — Xử lý báo cáo sai sót *(Phase 1)*

```
(Start) ← người dùng bấm "Báo cáo sai sót"
   │
   ├─[S] generation.classify-feedback   → lỗi retrieval / dữ liệu / guardrail / không phải lỗi
   │
   <G1> Mức độ nghiêm trọng
   │   ├─ NGHIÊM TRỌNG (trích dẫn văn bản hết hiệu lực)
   │   │    ├─[S] corpus.disable-chunk       → vô hiệu hoá NGAY
   │   │    └─[U] Xử lý trong 24h            (T: 24h → gọi trực on-call)
   │   └─ Thường ─[U] Hàng đợi review hàng tuần   (T: 7 ngày)
   │
   ├─[U] Chuyên gia xác nhận & sửa
   ├─[S] eval.add-to-golden-set
   ├─[S] generation.run-regression
   └─[S] corpus.notify-reporter → (End)
```

**Điểm thiết kế:** nhánh nghiêm trọng **vô hiệu hoá chunk trước, điều tra sau**. Trong domain này, để người dùng tiếp tục nhận trích dẫn hết hiệu lực trong lúc chờ điều tra là rủi ro lớn hơn nhiều so với việc tạm mất một mẩu nội dung.

---

## 3. P3 — Dẫn dắt quy trình XNK có trạng thái *(Phase 1)*

**Đây là điểm khác biệt sản phẩm** — biến hệ thống từ hỏi–đáp thành trợ lý quy trình.

### 3.1 Cơ chế

```
Người dùng hỏi "tôi cần làm gì để xin xác định trước mã số hàng hóa?"
   → Hệ thống trả lời (RAG thường)
   → Đề nghị: "Bạn có muốn tôi mở quy trình theo dõi không?"
   → Đồng ý → chat-orchestrator gọi tool khoi_tao_quy_trinh()
   → Camunda tạo process instance, businessKey = <user_id>:<ma_lo_hang>
   → Mỗi lần người dùng quay lại, hệ thống truy vấn instance và biết đang ở bước nào
   → UI hiển thị bằng TelerikStepper
```

### 3.2 Ba quy trình cho v1 🔶

| Quy trình | Các bước dự kiến — **cần chuyên gia xác nhận** |
|---|---|
| **Xin xác định trước mã số hàng hóa** | Chuẩn bị hồ sơ → Nộp → Chờ kết quả *(timer theo thời hạn luật định)* → Nhận thông báo |
| **Chuẩn bị bộ chứng từ khai báo** | Xác định mã loại hình → Sinh checklist chứng từ theo loại hình → Theo dõi chứng từ đã có / còn thiếu → Sẵn sàng khai |
| **Xin C/O theo FTA** | Xác định FTA áp dụng → Xác định quy tắc xuất xứ → Chuẩn bị hồ sơ chứng minh → Nộp → Theo dõi |

**⚠️ BA không tự viết các bước và thời hạn luật định của ba quy trình này.** Chúng phải được chuyên gia mô tả theo thực tế và đối chiếu với văn bản. Bảng trên chỉ là khung để mở đầu buổi làm việc.

---

## 4. P4 — Onboarding tenant B2B *(Phase 1)*

```
(Start) ← Sales báo có khách hàng mới
   ├─[U] Ký hợp đồng
   ├─[S] tenant.create-organization
   ├─[U] Cấu hình SSO cùng IT của khách hàng     (T: 14 ngày)
   ├─[U] Nạp corpus riêng của khách *(nếu có)*
   ├─[U] Đào tạo người dùng
   ├─[S] tenant.activate
   └─(End)
```

---

## 5. KHÔNG dùng Camunda ở những chỗ này

Nhắc lại để đội ngũ không bị cám dỗ mở rộng:

| Luồng | Vì sao không | Dùng gì |
|---|---|---|
| Hỏi–đáp thường | Đồng bộ dưới 10 giây, không có bước con người | REST trực tiếp |
| Sinh embedding hàng loạt | Thuần kỹ thuật, không rẽ nhánh nghiệp vụ | SQS + worker |
| Crawl định kỳ | Chỉ là lịch chạy | EventBridge Scheduler — chỉ khi *phát hiện văn bản mới* mới tạo instance P1 |
| Retry khi gọi API lỗi | Mối quan tâm hạ tầng | Polly / tenacity + DLQ |
| Điều phối request giữa microservice | Camunda không phải service mesh | ECS Service Connect |

---

## 6. Giao diện User Task

**Không dùng Camunda Tasklist mặc định.** User Task được render bằng UI Blazor + Telerik của hệ thống, gọi Camunda REST API. Lý do: chuyên gia XNK làm việc trong một giao diện thống nhất, không phải học thêm công cụ thứ hai — và giao diện review phạm vi ảnh hưởng cần hiển thị nội dung Điều/Khoản song song, việc Tasklist mặc định không làm được.

| User Task | Màn hình | Component |
|---|---|---|
| Sửa cấu trúc thủ công | Cây Điều/Khoản có thể sửa, đối chiếu văn bản gốc | `TelerikTreeList` + `TelerikSplitter` |
| ★ Review phạm vi ảnh hưởng | Danh sách Điều bị đề xuất tác động, có thể thêm/bớt, xem nội dung cũ–mới | `TelerikGrid` + `TelerikSplitter` |
| Phê duyệt đưa vào corpus | Tóm tắt văn bản, checklist, nút duyệt/từ chối kèm lý do | `TelerikWindow` |

Camunda Cockpit chỉ dùng cho vận hành, đặt sau ALB nội bộ + VPN.

---

## 7. Kiểm chứng

| Mức | Cách |
|---|---|
| External Task Worker | Unit test từng worker, mock Camunda REST |
| Quy trình đầu-cuối | `camunda-bpm-assert` với engine in-memory, deploy `.bpmn` thật |
| Ràng buộc External Task | `ci-bpmn.yml` quét file `.bpmn`, chặn Java Delegate ⚙️ |
| Timer escalation | Tua nhanh đồng hồ engine trong test |
| Đúng nghiệp vụ | **Chuyên gia XNK đi qua toàn bộ quy trình trên môi trường dev** |

---

## 8. Phê duyệt

| Vai trò | Người | Ngày | Chữ ký |
|---|---|---|---|
| BA (soạn) | | | |
| Chuyên gia XNK (xác nhận luồng nghiệp vụ) | | | |
| Tech Lead (xác nhận khả thi kỹ thuật) | | | |
| Product Owner | | | |
