# Kế hoạch phỏng vấn người dùng

> **Trạng thái: ✅ Kịch bản hoàn thiện — thực hiện Sprint 0–1.**
> Sở hữu: BA · Kết quả: `docs/nghien-cuu/` + bổ sung vào [`03`](03-pham-vi-nghiep-vu-v1.md) và golden set

---

## 1. Mục tiêu

Phỏng vấn này **không** nhằm hỏi *"bạn có muốn dùng chatbot không?"* — câu đó luôn nhận được câu trả lời lịch sự và vô dụng.

Ba mục tiêu thật:

| # | Mục tiêu | Đầu ra |
|---|---|---|
| 1 | Thu thập **câu hỏi thật** người dùng đang gặp | Bổ sung golden set |
| 2 | Hiểu **quy trình làm việc hiện tại** — họ tra cứu bằng cách nào hôm nay | Xác thực phạm vi [`03`](03-pham-vi-nghiep-vu-v1.md) và quy trình P3 |
| 3 | Xác định **hậu quả của câu trả lời sai** trong công việc của họ | Hiệu chỉnh mức độ thận trọng của guardrail |

Mục tiêu 3 là mục tiêu ít người nghĩ tới nhưng quan trọng nhất với dự án này. Nó cho biết ngưỡng an toàn phải đặt ở đâu.

---

## 2. Đối tượng & lịch

| Nhóm | Người | Số buổi | Thời lượng | Sprint |
|---|---|---|---|---|
| **P1 — Khai báo viên** (S17) | 3 người: 1 dưới 2 năm KN, 1 khoảng 5 năm, 1 trên 10 năm | 3 | 45' | Sprint 0 |
| **P2 — Nhân viên chứng từ/logistics** | 2 người | 2 | 45' | Sprint 1 |
| **P3 — Học viên** (S19) | 1 giảng viên + 3 học viên *(phỏng vấn nhóm)* | 2 | 60' | Sprint 1 |
| **Khách hàng B2B** (S18) | 1–2 đại diện | 2 | 45' | Sprint 1 |

**Chọn cả người ít kinh nghiệm lẫn nhiều kinh nghiệm có chủ đích.** Người mới cho biết hệ thống cần giải thích tới mức nào; người cũ cho biết những ca khó mà hệ thống dễ sai.

---

## 3. Kịch bản — P1 Khai báo viên (45 phút)

### Mở đầu (5')

> *"Chúng tôi đang tìm hiểu cách anh/chị tra cứu quy định trong công việc hàng ngày, để xem có thể hỗ trợ được gì. Không có câu trả lời đúng hay sai, và tôi quan tâm tới cách anh/chị thực sự làm chứ không phải cách lý thuyết. Tôi ghi chép được không?"*

### Phần A — Bối cảnh (5')

1. Anh/chị làm ở vị trí này bao lâu rồi?
2. Một ngày làm việc điển hình xử lý khoảng bao nhiêu tờ khai?
3. Loại hàng hóa nào là chủ yếu?

### Phần B — Câu hỏi thật *(quan trọng nhất — 15')*

> **Kỹ thuật:** hỏi về **lần gần nhất**, không hỏi về thói quen chung. Câu hỏi khái quát cho ra câu trả lời đã được làm mượt; câu hỏi cụ thể cho ra dữ liệu thật.

4. **Lần gần đây nhất anh/chị phải tra cứu một quy định là khi nào? Tra cái gì?**
5. Lúc đó anh/chị tra bằng cách nào? *(theo dõi: Google, hỏi đồng nghiệp, gọi hải quan, tra văn bản gốc)*
6. Mất bao lâu?
7. **Có tìm được không? Nếu không thì làm gì tiếp?**
8. Trong tháng vừa rồi còn lần nào tương tự không?
9. Có câu hỏi nào anh/chị phải tra đi tra lại nhiều lần không? *(→ ứng viên cho semantic cache)*

### Phần C — Hiệu lực văn bản *(kiểm chứng giả định cốt lõi — 10')*

10. **Anh/chị có bao giờ áp dụng nhầm một quy định đã bị sửa đổi chưa?**
11. Nếu có: chuyện gì xảy ra? *(hậu quả thật)*
12. Làm sao anh/chị biết một Thông tư còn hiệu lực hay đã bị sửa?
13. Khi tra Google ra một bài viết, làm sao biết nó còn đúng không?

> Nếu người được phỏng vấn kể ra được ca cụ thể ở câu 10–11, **ghi lại nguyên văn**. Đây là bằng chứng mạnh nhất cho luận điểm của dự án và là chất liệu tốt nhất cho buổi Go/No-Go.

### Phần D — Hậu quả của sai sót (5')

14. Nếu tra sai một quy định thì hậu quả thường là gì? *(theo dõi: tờ khai bị bác, lưu kho, phạt, mất khách)*
15. Chi phí ước tính của một lần như vậy?
16. **Giữa "trả lời nhanh nhưng có thể sai" và "nói không biết", anh/chị muốn công cụ làm gì?**

> Câu 16 hiệu chỉnh trực tiếp guardrail ⑤. Nếu đa số chọn "nói không biết", ngưỡng từ chối đặt cao.

### Phần E — Quy trình có trạng thái (5')

17. Có thủ tục nào anh/chị phải theo dõi qua nhiều ngày, nhiều bước không? *(→ xác thực Camunda P3)*
18. Hiện anh/chị theo dõi bằng gì? *(Excel, giấy, trí nhớ)*
19. Có bao giờ quên mất đang ở bước nào hoặc lỡ hạn không?

### Kết (5')

20. Nếu có một công cụ tra cứu, điều gì khiến anh/chị **không** dùng nó?
21. Anh/chị có sẵn sàng dùng thử và cho phản hồi trong 2 tháng tới không?

> Câu 20 là câu hỏi ngược có chủ đích. Hỏi *"điều gì khiến anh/chị dùng"* nhận được câu trả lời lịch sự; hỏi *"điều gì khiến anh/chị không dùng"* nhận được sự thật.

---

## 4. Kịch bản rút gọn — P3 Học viên (60', nhóm)

Nhóm này kiểm chứng **ngưỡng an toàn** của hệ thống.

1. Các bạn đang học/làm ở giai đoạn nào?
2. Khi gặp thuật ngữ không hiểu, các bạn làm gì?
3. **Nếu một công cụ trả lời rất tự tin, các bạn có kiểm tra lại không?** Kiểm tra bằng cách nào?
4. Đưa 3 câu trả lời mẫu — 1 đúng, 1 sai tinh vi, 1 trích văn bản hết hiệu lực. **Các bạn có phân biệt được không?**
5. Điều gì trong câu trả lời khiến các bạn tin tưởng? *(→ thiết kế cách hiển thị trích dẫn)*
6. Nếu câu trả lời có ghi "theo Điều 18 Thông tư 38", các bạn có bấm vào xem không?

> **Bài tập ở câu 4 là phần giá trị nhất của buổi này.** Nếu học viên không phân biệt được câu trả lời trích văn bản hết hiệu lực với câu đúng, đó là bằng chứng trực tiếp rằng badge trạng thái hiệu lực (E5-05) không phải trang trí mà là tính năng an toàn.

---

## 5. Kịch bản — Khách hàng B2B (45')

1. Doanh nghiệp anh/chị xử lý bao nhiêu tờ khai một tháng?
2. Đội ngũ khai báo có bao nhiêu người? Kinh nghiệm trung bình?
3. Chi phí lớn nhất phát sinh từ sai sót nghiệp vụ trong năm qua là gì?
4. Doanh nghiệp có tài liệu quy trình nội bộ (SOP) không? *(→ xác thực nhu cầu corpus riêng, câu Q2 trong [`03`](03-pham-vi-nghiep-vu-v1.md) §7)*
5. Nếu có công cụ như vậy, ai trong công ty sẽ dùng?
6. Anh/chị có yêu cầu gì về bảo mật dữ liệu? *(→ đầu vào cho CISO)*
7. **Anh/chị có sẵn sàng làm design partner — dùng thử sớm và cho phản hồi định kỳ không?**

> Câu 7 hướng tới việc lấy **cam kết bằng văn bản** (dù chỉ là thư ngỏ ý). Một prototype có khách hàng thật đang chờ dùng là lập luận mạnh hơn nhiều tại buổi Go/No-Go.

---

## 6. Quy tắc thực hiện

| Quy tắc | Lý do |
|---|---|
| **Không mô tả giải pháp trước khi hỏi xong** | Người được phỏng vấn sẽ trả lời theo hướng chiều lòng giải pháp |
| **Hỏi về hành vi đã xảy ra, không hỏi về ý định** | *"Lần gần nhất bạn làm gì"* đáng tin hơn *"bạn có dùng không"* |
| **Im lặng sau câu hỏi** | Thông tin giá trị thường đến sau 3 giây im lặng |
| **Ghi nguyên văn, không diễn giải khi ghi** | Diễn giải sau, khi phân tích |
| **Hai người: một hỏi, một ghi** | Người hỏi không ghi chép được đầy đủ |
| Xin phép ghi âm; nếu bị từ chối thì tôn trọng | |
| Không quá 45–60 phút | Sau đó chất lượng câu trả lời giảm rõ rệt |

---

## 7. Xử lý kết quả

| Đầu ra | Đích đến |
|---|---|
| Câu hỏi thật thu được | Golden set ([`14`](14-phuong-phap-golden-set.md)) |
| Ca áp dụng nhầm quy định hết hiệu lực | **Báo cáo Go/No-Go** — bằng chứng cho luận điểm dự án |
| Cách họ tra cứu hiện tại | Xác thực phạm vi [`03`](03-pham-vi-nghiep-vu-v1.md) |
| Thủ tục nhiều bước họ đang theo dõi thủ công | Xác thực Camunda P3 ([`10`](10-dac-ta-quy-trinh-bpmn.md) §3) |
| Ngưỡng chấp nhận sai sót | Hiệu chỉnh guardrail ⑤ |
| Yêu cầu bảo mật của khách B2B | Đầu vào cho CISO (S10) |
| Điều khiến họ không dùng | Backlog rủi ro áp dụng |

**Báo cáo tổng hợp:** 3 trang, gồm — 5 phát hiện chính · 3 trích dẫn nguyên văn đắt giá nhất · danh sách câu hỏi thu được · thay đổi đề xuất cho phạm vi. Trình bày tại Sprint Review.

---

## 8. Mẫu ghi chép

```
Buổi phỏng vấn: <mã>          Ngày:            Người hỏi / Người ghi:
Đối tượng: <nhóm> · <kinh nghiệm> · <đơn vị>

── Câu hỏi thật thu được ─────────────────────
1.
2.

── Cách tra cứu hiện tại ─────────────────────

── ⚠️ Ca sai sót do quy định hết hiệu lực ────
   (ghi nguyên văn nếu có)

── Hậu quả của sai sót ───────────────────────

── Thủ tục nhiều bước đang theo dõi thủ công ──

── Trích dẫn nguyên văn đáng nhớ ─────────────
   "

── Điều khiến họ KHÔNG dùng ──────────────────

── Việc cần làm tiếp ─────────────────────────
```
