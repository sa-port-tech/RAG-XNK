# Kế hoạch triển khai Prototype theo Scrum

> Tài liệu này do **Product Owner** sở hữu. Kế hoạch kỹ thuật: [`00-ke-hoach-tong-the.md`](00-ke-hoach-tong-the.md).
> Phạm vi: Phase 0.5 — Prototype 7 tuần trên AWS Free Tier.

---

## 1. Product Goal

> **Trong 7 tuần, chứng minh (hoặc bác bỏ) rằng hệ thống có thể trả lời câu hỏi nghiệp vụ XNK kèm trích dẫn chính xác tới Điều/Khoản, và không bao giờ dẫn chiếu tới điều khoản đã hết hiệu lực — đủ để quyết định có đầu tư ~$12.200/tháng cho production hay không.**

Đây không phải mục tiêu "làm ra một demo đẹp". Prototype này tồn tại để **giảm rủi ro của một quyết định đầu tư**, và mọi thứ trong kế hoạch dưới đây phục vụ mục tiêu đó.

Hệ quả trực tiếp: nếu phải chọn giữa "thêm một tính năng cho demo trông ấn tượng hơn" và "làm chắc bộ lọc hiệu lực", PO chọn cái thứ hai — mỗi lần.

---

## 2. Đội ngũ

### 2.1 Thành phần

| Vai trò | Mức tham gia | FTE-tháng (7 tuần) | Ghi chú |
|---|---|---|---|
| **Product Owner** | 50% | 0,88 | Sở hữu backlog, nghiệm thu, quyết định phạm vi |
| **Scrum Master** | 25% | 0,44 | **Không kiêm Tech Lead** — tránh xung đột vai trò |
| **Business Analyst / Process Analyst** | 60% | 1,05 | Chi tiết hoá story + **mô hình hoá BPMN** + tài liệu nghiệp vụ |
| **Tech Lead / Kiến trúc sư** | 100% | 1,75 | Kiêm phát triển .NET |
| **Data Engineer** | 100% | 1,75 | **Vị trí quan trọng nhất** — corpus & đồ thị hiệu lực |
| **Backend .NET** | 100% | 1,75 | |
| **AI / Python Engineer** | 100% | 1,75 | Retrieval + Generation |
| **Frontend Blazor** | 60% | 1,05 | 30% tuần 1–3, 100% tuần 4–7 |
| **DevOps / Platform** | 60% | 1,05 | 100% tuần 1–2, 40% sau đó |
| **Chuyên gia XNK** (2 người × 20%) | 40% | 0,70 | **Không phải nhân sự tuỳ chọn** |
| | | **≈ 12,2 FTE-tháng** | |

Team Scrum (Developers) là 7 người — nằm trong khoảng khuyến nghị 3–9.

Chi tiết trách nhiệm từng vai trò, yêu cầu tuyển dụng, và **ranh giới PO / BA**: xem [`02-stakeholder-va-nhan-su.md`](02-stakeholder-va-nhan-su.md) §11.

### 2.2 Vì sao Scrum Master không kiêm Tech Lead

Tech Lead có động cơ tự nhiên là bảo vệ quyết định kỹ thuật của mình; Scrum Master có nhiệm vụ tạo không gian để đội ngũ chất vấn chính những quyết định đó. Gộp hai vai trò làm Retrospective mất tác dụng. Nếu tổ chức không có SM rảnh, mượn SM từ team khác 25% thời gian — rẻ hơn nhiều so với cái giá của một Retrospective vô nghĩa.

### 2.3 Điều PO sẽ bảo vệ dưới áp lực

Bốn thứ luôn bị cắt đầu tiên khi tiến độ căng, và cả bốn đều không được cắt:

1. **Thời gian của chuyên gia XNK.** Không có họ, đội ngũ kỹ thuật không có cách nào biết hệ thống đúng hay sai. Lịch của họ được chốt từ Sprint 0 và đưa vào cam kết bằng văn bản.
2. **Chất lượng model.** Không hạ Opus 5 để tiết kiệm vài chục đô. Hạ model rồi kết luận "RAG cho pháp luật không khả thi" là thay nhầm biến số và rút ra kết luận sai về cả dự án.
3. **Chất lượng golden set hơn số lượng.** 40 câu chuyên gia viết cẩn thận có giá trị hơn 200 câu sinh tự động.
4. **Chuyên gia có mặt trong Sprint Review.** Demo mà không có người biết nghiệp vụ ngồi xem thì không phải nghiệm thu, chỉ là trình chiếu.

### 2.4 RACI cho các quyết định chính

| Quyết định | R | A | C | I |
|---|---|---|---|---|
| Phạm vi Sprint | PO | PO | Team | Stakeholder |
| Chi tiết hoá story & AC | **BA** | PO | Chuyên gia XNK | Team |
| Mô hình hoá quy trình BPMN | **BA** | PO | Chuyên gia XNK, Tech Lead | Team |
| Kiến trúc kỹ thuật | Tech Lead | Tech Lead | Team, PO | Stakeholder |
| Nội dung nghiệp vụ đúng/sai | Chuyên gia XNK | PO | Team | — |
| Golden set & tiêu chí chấm | Chuyên gia XNK | PO | AI Eng | Team |
| Go / No-Go cuối kỳ | PO | **Stakeholder tài trợ** | Tech Lead, Chuyên gia | Toàn bộ |
| Chi tiêu AWS vượt ngưỡng | DevOps | PO | Tech Lead | Stakeholder |

---

## 3. Cấu trúc Sprint

**7 tuần = Sprint 0 (1 tuần) + 3 Sprint × 2 tuần.**

Mỗi Sprint có một **Sprint Goal viết thành một câu**, và một increment **demo được cho người ngoài đội ngũ**.

| Sprint | Tuần | Sprint Goal |
|---|---|---|
| **0** | 1 | *"Đội ngũ merge được một PR và thấy nó tự động chạy trên môi trường dev AWS."* |
| **1** | 2–3 | *"Chứng minh bằng truy vấn SQL rằng hệ thống phân biệt đúng điều khoản còn/hết hiệu lực trên cặp TT 38/2015 ↔ TT 39/2018."* |
| **2** | 4–5 | *"Trả lời được câu hỏi nghiệp vụ thật kèm trích dẫn chính xác, và không bao giờ trích văn bản đã hết hiệu lực."* |
| **3** | 6–7 | *"Chuyên gia XNK tự dùng hệ thống qua giao diện thật và chấm được điểm chất lượng."* |

Ba Sprint Goal này xếp theo thứ tự **rủi ro giảm dần**. Sprint 1 tấn công thẳng vào giả định nguy hiểm nhất của cả dự án. Nếu nó thất bại, ta biết ở tuần thứ 3 chứ không phải tuần thứ 7 — và tiết kiệm được 4 tuần của cả đội.

### 3.1 Về ước lượng — nói thẳng

Với 3 Sprint, **velocity không có ý nghĩa thống kê**. Đừng dùng nó để cam kết phạm vi hay đánh giá đội ngũ.

Thay vào đó, mỗi Sprint chia backlog thành **Must-have** (nếu không xong thì Sprint Goal thất bại) và **Should-have** (làm nếu còn thời gian). PO cam kết với stakeholder về **Sprint Goal**, không cam kết về danh sách story.

Sprint 0 dùng để hiệu chỉnh cảm giác về tốc độ. Sau Sprint 1, PO điều chỉnh phạm vi Sprint 2–3 dựa trên dữ liệu thật.

---

## 4. Product Backlog

### 4.1 Epic

| ID | Epic | Sprint | Ưu tiên |
|---|---|---|---|
| E1 | Nền tảng hạ tầng & CI/CD | 0 | P0 |
| E2 | Corpus & Đồ thị hiệu lực | 1 | **P0 — rủi ro cao nhất** |
| E3 | Truy xuất (hybrid search + lọc hiệu lực) | 2 | P0 |
| E4 | Sinh câu trả lời & Guardrails | 2 | P0 |
| E5 | Giao diện & Multi-tenant | 3 | P1 |
| E6 | Quy trình Camunda P1 | 3 | P1 |
| E7 | Đánh giá chất lượng | xuyên suốt | P0 |
| E8 | Spike (OCR, streaming Blazor) | 1–2 | P2 |

### 4.2 Story mẫu — định dạng chuẩn của dự án

Mỗi story viết theo `Là <vai trò>, tôi muốn <hành động>, để <giá trị>` và có Acceptance Criteria dạng Given/When/Then **kiểm chứng được bằng máy**.

---

**`E2-01` — Truy vấn hiệu lực theo thời điểm** · P0 · 8 điểm

> Là **chuyên gia nghiệp vụ**, tôi muốn hệ thống biết Điều 18 TT 38/2015 đã bị TT 39/2018 sửa đổi, để tôi không bị dẫn chiếu tới nội dung đã lỗi thời.

**AC**
- Given ngày tham chiếu là hôm nay, When gọi `hieu_luc_tai_ngay(dieu_18_tt38)`, Then trả về `false`
- Given ngày tham chiếu `2017-01-01`, When gọi hàm đó, Then trả về `true`
- Bảng `sua_doi` ghi nhận quan hệ `TT39 → sua_doi → TT38` với `pham_vi_anh_huong` liệt kê đúng danh sách Điều bị tác động
- Chuyên gia XNK xác nhận danh sách Điều bị tác động là đúng

---

**`E2-02` — Phân rã cấu trúc pháp lý** · P0 · 13 điểm

> Là **hệ thống truy xuất**, tôi cần mỗi Khoản/Điểm là một bản ghi có đường dẫn cấu trúc đầy đủ, để trích dẫn chỉ đúng tới cấp Điều/Khoản/Điểm.

**AC**
- 15 văn bản được phân rã thành bản ghi `dieu_khoan` có `duong_dan_cau_truc` đầy đủ
- Số Điều liên tục, không nhảy cóc; sai lệch được đưa vào hàng đợi review
- Chuyên gia review ngẫu nhiên 10% → độ chính xác cấu trúc ≥ 98%

---

**`E3-01` — Lọc hiệu lực trong truy xuất** · P0 · 8 điểm

> Là **khai báo viên**, tôi muốn kết quả tra cứu không bao giờ chứa điều khoản đã hết hiệu lực, để không nộp tờ khai theo quy định cũ.

**AC**
- `Stale Citation Rate = 0` trên bộ 10 câu bẫy
- Bộ lọc thực thi **trong câu SQL**, không ở tầng application — verify bằng code review và test
- Given câu hỏi chứa mốc thời gian *"tháng 3/2023"*, When truy vấn, Then kết quả phản ánh trạng thái pháp luật tại thời điểm đó

---

**`E4-03` — Guardrail mã HS** · P0 · 5 điểm

> Là **người dùng hỏi về mã HS**, tôi muốn được cảnh báo rõ rằng đây chỉ là tham khảo, để không tự ý áp mã và bị bác tờ khai.

**AC**
- 100% câu trả lời có chứa mã HS đều kèm câu cảnh báo về thẩm quyền cơ quan hải quan — **test tự động**
- Hệ thống chủ động nêu thủ tục xác định trước mã số hàng hóa
- Không có câu trả lời nào dùng ngôn ngữ khẳng định (*"mã HS là…"*) mà không có cảnh báo

---

**`E4-05` — Từ chối khi không đủ căn cứ** · P0 · 5 điểm

> Là **người dùng**, tôi muốn hệ thống nói thẳng là không biết thay vì bịa, để tôi biết khi nào cần hỏi người thật.

**AC**
- Given 20 câu hỏi ngoài phạm vi corpus, When hỏi, Then ≥ 18 câu được từ chối đúng cách
- Câu từ chối có đề xuất kênh thay thế
- Không câu nào trả lời từ kiến thức nền của model — verify bằng LLM-as-judge + human spot check

---

**`E5-04` — Cách ly dữ liệu giữa tenant** · P0 · 5 điểm

> Là **khách hàng B2B**, tôi cần chắc chắn tài liệu nội bộ của tôi không bao giờ xuất hiện trong câu trả lời cho khách hàng khác.

**AC**
- Test tự động: user tenant A truy vấn không bao giờ nhận chunk của tenant B
- Bộ lọc `tenant_id` nằm trong mệnh đề WHERE của truy vấn vector, không ở tầng application
- Test này là **required status check** trong CI

---

**`E8-01` — Spike: đo chất lượng OCR** · P2 · 5 điểm · *timeboxed 3 ngày*

> Là **Product Owner**, tôi cần số liệu về độ chính xác OCR để ước lượng công sức Phase 1.

**AC**
- 3–5 văn bản scan tiêu biểu chạy qua VietOCR, PaddleOCR-vi, Amazon Textract
- Báo cáo có **hai** con số: độ chính xác ký tự, và **thời gian chuyên gia phải sửa lại**
- Kết quả **không nối vào pipeline chính**
- Timebox cứng 3 ngày — hết giờ thì báo cáo những gì đã đo được

---

Backlog đầy đủ ước tính **55–70 story**, quản lý trên GitHub Projects. Story mẫu ở trên minh hoạ định dạng bắt buộc.

### 4.3 Ngoài phạm vi prototype — chốt để chống scope creep

Danh sách này PO công bố ở Sprint 0 và **không thương lượng trong sprint**:

SSO SAML/OIDC · Crawler tự động · Quy trình Camunda P2/P3/P4 · Semantic cache · Batches API · Multi-AZ / HA · Blue-green deployment · Môi trường staging & production · Mobile app · Xuất báo cáo · Tích hợp hệ thống khai báo hải quan bên ngoài · Quản lý người dùng nâng cao · Đa ngôn ngữ.

Đề xuất phát sinh trong sprint đi vào **Backlog Phase 1**, không chen vào sprint đang chạy.

---

## 5. Definition of Ready / Definition of Done

### 5.1 Definition of Ready

Story chỉ được đưa vào Sprint Planning khi:

- [ ] Viết theo định dạng `Là… tôi muốn… để…`
- [ ] Có AC dạng Given/When/Then, **kiểm chứng được bằng máy hoặc bằng chuyên gia**
- [ ] Đã ước lượng story point
- [ ] Không phụ thuộc vào story chưa hoàn thành ngoài sprint hiện tại
- [ ] **Nếu chạm nghiệp vụ XNK: chuyên gia đã review và xác nhận AC đúng nghiệp vụ** ← đặc thù dự án này

Điều kiện cuối là điều kiện hay bị bỏ qua nhất và tốn kém nhất khi bỏ qua. Một story được code đúng theo AC sai về nghiệp vụ là công sức bỏ đi hoàn toàn.

### 5.2 Definition of Done

Story chỉ được tính hoàn thành khi:

- [ ] PR được review, ≥1 approval (≥2 nếu chạm `infra/` hoặc `db/migrations/`)
- [ ] Unit test pass, coverage không giảm
- [ ] Toàn bộ CI xanh
- [ ] Nếu chạm `prompts/`, `retrieval/`, `generation/`: **`eval-regression` pass, `Stale Citation Rate = 0`**
- [ ] Đã deploy được lên môi trường dev
- [ ] Tài liệu liên quan đã cập nhật (ADR nếu là quyết định kiến trúc)
- [ ] **PO nghiệm thu trên môi trường dev**, không phải trên máy dev
- [ ] Nếu chạm nghiệp vụ: **chuyên gia XNK xác nhận**

---

## 6. Nghi thức

| Nghi thức | Tần suất | Thời lượng | Tham gia |
|---|---|---|---|
| Daily Standup | Hàng ngày 9:15 | 15' | Developers + SM (PO tuỳ chọn) |
| Sprint Planning | Đầu sprint | 2h | Toàn bộ + chuyên gia XNK |
| Backlog Refinement | Giữa sprint (thứ 4 tuần 1) | 1h | PO + Developers + chuyên gia |
| **Expert Review Session** | **Hàng tuần, thứ 5** | **1,5h** | **Chuyên gia XNK + Data Eng + AI Eng** |
| Sprint Review (Demo) | Cuối sprint | 1h | Toàn bộ + stakeholder + **chuyên gia (bắt buộc)** |
| Sprint Retrospective | Cuối sprint | 1h | Developers + SM |

**Expert Review Session là nghi thức PO thêm vào, không có trong Scrum chuẩn.** Lý do: vòng phản hồi nghiệp vụ không thể nén vào một buổi Sprint Review hai tuần một lần. Đồ thị hiệu lực, danh sách Điều bị sửa đổi, và đáp án golden set đều cần chuyên gia xác nhận **liên tục**. Nếu đợi tới Sprint Review mới phát hiện đồ thị sai, ta đã xây hai tuần trên nền sai.

Tổng thời gian nghi thức: ~5,5h/tuần/người. Với sprint 2 tuần, đây là mức hợp lý — không quá nặng cho đội 7 người.

---

## 7. Ngân sách

### 7.1 Cấu trúc

| Khoản mục | Đơn vị | Ghi chú |
|---|---|---|
| **Nhân sự** | 12,2 FTE-tháng | Chiếm >99% ngân sách |
| Hạ tầng AWS | $0 | Trong phạm vi credit (§3.6 kế hoạch tổng thể) |
| LLM (Bedrock) | $33–65 | Cũng trong phạm vi credit |
| Telerik UI for Blazor | $0 | Trial 30 ngày, kích hoạt đầu Sprint 3 |
| GitHub | $0–63 | Free tier; nếu cần Team: $4/user/tháng × 9 |
| **Dự phòng** | 15% | |

### 7.2 Ví dụ quy đổi

*Đơn giá dưới đây là **giả định để minh hoạ cách tính** — thay bằng đơn giá thực tế của tổ chức bạn. Đơn giá là chi phí toàn bộ cho người sử dụng lao động (lương gộp + BHXH + overhead).*

| Vai trò | FTE-tháng | Đơn giá giả định (tr VNĐ/tháng) | Thành tiền (tr VNĐ) |
|---|---|---|---|
| Product Owner | 0,88 | 60 | 52,5 |
| Scrum Master | 0,44 | 45 | 19,7 |
| **Business Analyst** | **1,05** | **45** | **47,3** |
| Tech Lead / Kiến trúc sư | 1,75 | 75 | 131,3 |
| Data Engineer | 1,75 | 55 | 96,3 |
| Backend .NET | 1,75 | 50 | 87,5 |
| AI / Python Engineer | 1,75 | 60 | 105,0 |
| Frontend Blazor | 1,05 | 40 | 42,0 |
| DevOps / Platform | 1,05 | 50 | 52,5 |
| Chuyên gia XNK (2 × 20%) | 0,70 | 35 | 24,5 |
| **Cộng nhân sự** | **12,2** | | **658,6** |
| Hạ tầng + công cụ | | | ~2 |
| Dự phòng 15% | | | 99,1 |
| **TỔNG** | | | **≈ 760 tr VNĐ** (~$29.000) |

### 7.3 Điều PO muốn bạn nhìn thấy trong bảng này

**Nhân sự chiếm hơn 99% ngân sách prototype. Hạ tầng chiếm chưa tới 0,3%.**

Cuộc trao đổi trước tập trung nhiều vào việc đưa chi phí hạ tầng từ $150 xuống $0. Đó là việc đúng nên làm — nhưng nó tối ưu 0,3% của bài toán. Đòn bẩy thật nằm ở chỗ khác:

- **Rút ngắn 1 tuần** = tiết kiệm ~94tr VNĐ. Lớn hơn toàn bộ chi phí hạ tầng của cả dự án gấp hàng trăm lần.
- **Phát hiện kiến trúc sai ở tuần 3 thay vì tuần 7** = tiết kiệm ~376tr VNĐ. Đây chính là lý do Sprint 1 tấn công thẳng vào giả định rủi ro nhất.
- **Chuyên gia XNK không sẵn sàng** khiến sprint trượt = mất cả trăm triệu vì tiết kiệm vài chục triệu.

Ở production thì bức tranh đảo lại: hạ tầng + LLM ~$12.200/tháng ≈ 317tr VNĐ/tháng, tương đương chi phí của khoảng 6 kỹ sư. Lúc đó tối ưu hạ tầng mới thực sự đáng công.

---

## 8. Bộ tài liệu

| Tài liệu | Sở hữu | Hoàn thành | Nơi lưu |
|---|---|---|---|
| Kế hoạch tổng thể | PO | ✅ xong | `docs/00-…` |
| Kế hoạch Scrum (tài liệu này) | PO | ✅ xong | `docs/01-…` |
| Stakeholder & biên chế nhân sự | PO | ✅ xong | `docs/02-…` |
| **Phạm vi nghiệp vụ v1 (in/out)** | PO + chuyên gia | **Sprint 0 — chặn** | `docs/03-…` |
| **Danh mục 15 văn bản lõi** | Chuyên gia XNK | **Sprint 0 — chặn** | `docs/04-…` |
| **Rà soát bản quyền** | PO / Pháp chế | **Sprint 0 — chặn** | `docs/05-…` |
| **Golden set 40–50 câu** | Chuyên gia + PO | Sprint 0–1 | `eval/golden-set/` |
| ADR (Architecture Decision Record) | Tech Lead | Khi có quyết định | `docs/adr/` |
| Báo cáo spike OCR | Data Engineer | Sprint 1 | `docs/06-…` |
| Báo cáo eval mỗi sprint | AI Engineer | Cuối mỗi sprint | `docs/eval-reports/` |
| Runbook vận hành | DevOps | Sprint 3 | `docs/07-…` |
| **Báo cáo Go/No-Go** | PO | Tuần 7 | `docs/08-…` |

Ba tài liệu đánh dấu **chặn** phải xong trong Sprint 0. Không có chúng thì Sprint 1 không có gì để làm.

### 8.1 ADR — ghi lại quyết định kiến trúc

Dự án đã có nhiều quyết định đánh đổi mà lý do sẽ bị quên sau vài tháng. Mỗi cái cần một ADR ngắn (1 trang: bối cảnh · quyết định · hệ quả · phương án đã cân nhắc và loại bỏ):

| ADR | Nội dung |
|---|---|
| 001 | Blazor WebAssembly thay vì Blazor Server |
| 002 | Camunda 7 chỉ dùng External Task pattern |
| 003 | 7 microservice và tiêu chí tách thêm |
| 004 | `retrieval` được đọc trực tiếp schema `corpus` — ngoại lệ có chủ đích |
| 005 | Bedrock cho prototype, Claude Platform on AWS cho production |
| 006 | Chunking theo Điều, không fixed-size |
| 007 | Theo dõi hiệu lực ở cấp Điều/Khoản, không chỉ cấp văn bản |
| 008 | Corpus không nằm trong git |

ADR ngăn việc sáu tháng sau có người "tối ưu" bằng cách gỡ bỏ một quyết định mà họ không biết lý do.

---

## 9. Chỉ số theo dõi

### 9.1 Chỉ số sản phẩm — PO đánh giá bằng nhóm này

| Chỉ số | Nguồn | Mục tiêu tuần 7 |
|---|---|---|
| **Stale Citation Rate** | CI, tự động | **0** |
| Recall@10 | CI, tự động | ≥ 0,90 |
| Citation Precision | LLM-as-judge + human 20% | ≥ 0,90 |
| Correct Refusal Rate | CI, tự động | ≥ 0,90 |
| **Điểm chuyên gia XNK** | Chấm thủ công, thang 1–5 | **≥ 4,0** |

### 9.2 Chỉ số quy trình — dùng để cải tiến, không dùng để đánh giá người

Sprint Goal đạt/không · tỉ lệ story carry-over · số lượng defect phát hiện sau DoD · thời gian trung bình từ mở PR tới merge.

**Velocity không được dùng làm KPI.** Với 3 sprint nó không có ý nghĩa thống kê, và biến nó thành chỉ tiêu sẽ khiến đội ngũ thổi phồng ước lượng.

### 9.3 Chỉ số chi phí

Chi tiêu AWS thực tế so với credit còn lại (theo dõi hàng tuần) · token tiêu thụ theo ngày · số truy vấn trong sprint.

---

## 10. Sổ rủi ro

| # | Rủi ro | Khả năng | Tác động | Biện pháp |
|---|---|---|---|---|
| R1 | **Chuyên gia XNK không đủ thời gian** | Cao | **Rất cao** | Chốt lịch cố định từ Sprint 0, đưa vào cam kết bằng văn bản. Nếu không đảm bảo được → **kiến nghị hoãn dự án**, không chạy tiếp |
| R2 | **Đồ thị hiệu lực khó hơn dự kiến** | Trung bình | **Rất cao** | Bắt đầu chỉ với cặp TT38 ↔ TT39. Nếu một cặp không xong trong Sprint 1 → tín hiệu đỏ sớm, họp lại kiến trúc ngay |
| R3 | OCR ngốn hết Sprint 1 | Trung bình | Cao | Đã tách thành spike timebox 3 ngày, không nối pipeline |
| R4 | 7 microservice làm chậm tiến độ | Cao | Trung bình | Chấp nhận có chủ đích. Nếu Sprint 1 trượt Goal → cân nhắc gộp tạm 2 service Python, tách lại ở Phase 1 |
| R5 | Credit AWS không áp dụng như kỳ vọng | Trung bình | Trung bình | **Kiểm chứng ngay ngày 1–2 của Sprint 0.** Phương án B: 1 × EC2 `t4g.large` (~$54/tháng) |
| R6 | `vbpl.vn` chặn crawler | Trung bình | Thấp | Prototype tải thủ công 15 văn bản; crawler là việc của Phase 1 |
| R7 | Telerik trial hết hạn trước demo | Trung bình | Thấp | Kích hoạt đúng đầu Sprint 3 |
| R8 | Chất lượng tiếng Việt của BGE-M3 không đạt | Thấp | Cao | Sprint 2 đánh giá song song 2–3 model embedding trên golden set trước khi chốt |

R1 và R2 là hai rủi ro PO theo dõi hàng ngày. Cả hai đều có thể giết dự án, và cả hai đều lộ ra sớm nếu ta chú ý.

---

## 11. Quyết định Go / No-Go — tuần 7

### 11.1 Tiêu chí

**GO** khi đạt **tất cả**:
- `Stale Citation Rate = 0` trên bộ 10 câu bẫy
- `Recall@10 ≥ 0,90` trên golden set
- Chuyên gia XNK chấm **≥ 4,0/5** trên nhóm câu dễ + trung bình
- Test cách ly tenant pass
- Demo đầu-cuối chạy được: hỏi → trả lời có trích dẫn → bấm chip nhảy đúng đoạn văn bản gốc
- CI/CD tự động deploy lên dev hoạt động
- Báo cáo OCR có số liệu để ước lượng Phase 1

**CONDITIONAL GO**: đạt 5/7 tiêu chí, và có kế hoạch khắc phục cụ thể cho phần thiếu.

**NO-GO tuyệt đối** nếu: `Stale Citation Rate > 0`, **hoặc** chuyên gia chấm < 3,5.

Hai điều kiện NO-GO không thương lượng. Chúng là luận điểm cốt lõi của cả hệ thống — nếu không đạt ở quy mô 15 văn bản, chúng sẽ không tự tốt lên ở quy mô 150 văn bản.

### 11.2 Buổi họp quyết định

Tuần 7, 2 giờ. Tham gia: stakeholder tài trợ (người quyết) · PO (trình bày) · Tech Lead · chuyên gia XNK · Scrum Master.

Đầu vào: báo cáo Go/No-Go (`docs/08-…`) gửi trước 48 giờ, gồm số liệu từng chỉ số, demo ghi hình, báo cáo OCR, ước lượng Phase 1 đã hiệu chỉnh theo dữ liệu thật, và ngân sách production cập nhật.

Đầu ra: quyết định bằng văn bản, kèm lý do.

---

## 12. Tuần đầu tiên — việc PO làm ngay

| Ngày | Việc | Kết quả |
|---|---|---|
| 1 | Mở AWS account · **kiểm chứng điều khoản credit, đặc biệt loại trừ Marketplace** · thử một lời gọi Bedrock | Biết chắc credit có trả cho LLM không |
| 1 | Chốt lịch chuyên gia XNK bằng văn bản cho cả 7 tuần | Rủi ro R1 được kiểm soát |
| 2 | Họp chốt phạm vi nghiệp vụ v1 với chuyên gia | `docs/03-…` |
| 2 | Chốt danh mục 15 văn bản lõi | `docs/04-…` |
| 3 | Rà soát bản quyền (Incoterms/UCP) | `docs/05-…` |
| 3 | Dựng GitHub Project board, Issue Forms, CODEOWNERS, rulesets | Backlog có nơi để sống |
| 4 | Workshop golden set — vòng 1, mục tiêu 20 câu gồm 5 câu bẫy | Nền cho Sprint 1 |
| 4 | Viết backlog Sprint 1 theo DoR | Sprint Planning có nguyên liệu |
| 5 | Sprint 0 Review + Sprint 1 Planning | Sprint 1 khởi động |

Ba việc của ngày 1 quan trọng nhất. Nếu credit không áp dụng cho Bedrock, ngân sách thay đổi. Nếu chuyên gia không cam kết được lịch, **PO nên kiến nghị hoãn dự án thay vì bắt đầu** — chạy 7 tuần rồi phát hiện không ai xác nhận được đầu ra đúng hay sai là lãng phí 700 triệu.

---

## 13. Sau prototype

| Kết quả | Hành động |
|---|---|
| **GO** | Chuyển sang Phase 0 của kế hoạch tổng thể. Tái sử dụng toàn bộ: hạ tầng CDK, CI/CD, schema, code 7 service, golden set. Prototype **không phải đồ bỏ đi** — nó là nền của production. |
| **CONDITIONAL GO** | Sprint khắc phục 2 tuần, tập trung vào tiêu chí chưa đạt, rồi họp lại. |
| **NO-GO** | Họp phân tích nguyên nhân: vấn đề nằm ở kiến trúc, ở chất lượng dữ liệu, hay ở giới hạn của công nghệ hiện tại? Cân nhắc phương án khác (công cụ tra cứu có cấu trúc không dùng LLM; thu hẹp phạm vi xuống một lĩnh vực hẹp; hoãn chờ công nghệ trưởng thành hơn). **Kết thúc dự án tại đây với chi phí 760 triệu vẫn là kết quả tốt, nếu phương án còn lại là tiêu 3,7 tỷ mỗi năm cho hệ thống không dùng được.**
