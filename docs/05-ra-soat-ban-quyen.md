# Rà soát bản quyền & nghĩa vụ pháp lý của nội dung

> **Trạng thái: 🔶 Bản phân tích của BA — kết luận pháp lý thuộc Pháp chế (S12).**
> Sở hữu: BA (phân tích) + Pháp chế (kết luận) · Hạn: Sprint 0 (chặn Sprint 1)

---

## ⚠️ Giới hạn của tài liệu này

BA không phải luật sư. Tài liệu này **nêu vấn đề, phân tích rủi ro và đề xuất phương án** để Pháp chế có đủ thông tin ra kết luận. Ô "Kết luận Pháp chế" ở mỗi mục phải do S12 điền và ký.

**Không được ingest bất kỳ tài liệu nào ở mục 2 cho tới khi có kết luận bằng văn bản.**

---

## 1. Văn bản quy phạm pháp luật Việt Nam

| Nội dung | Luật · Nghị định · Thông tư · Quyết định · Công văn hướng dẫn của cơ quan nhà nước Việt Nam |
|---|---|
| **Phân tích của BA** | Theo hiểu biết chung, văn bản quy phạm pháp luật và văn bản hành chính của cơ quan nhà nước không thuộc đối tượng được bảo hộ quyền tác giả. Việc lưu trữ, trích dẫn toàn văn và sử dụng trong hệ thống tra cứu là hợp pháp. |
| **Rủi ro còn lại** | Nguồn *trung gian* (thuvienphapluat, luatvietnam…) có thể bổ sung phần tóm tắt, chỉ dẫn, đánh chỉ mục — những phần đó **là tài sản của họ**. |
| **Đề xuất** | Chỉ ingest từ nguồn nhà nước (`vbpl.vn`, `vanban.chinhphu.vn`, `customs.gov.vn`, `mof.gov.vn`, `moit.gov.vn`). Dùng nguồn trung gian **chỉ để đối chiếu**, không lưu nội dung của họ vào corpus. |
| **Kết luận Pháp chế** | *(S12 điền)* |

---

## 2. Tài liệu có bản quyền — KHÔNG được ingest

### 2.1 Incoterms® 2020 (ICC)

| | |
|---|---|
| Chủ sở hữu | International Chamber of Commerce |
| **Phân tích của BA** | Văn bản Incoterms® 2020 là ấn phẩm thương mại có bản quyền. "Incoterms" còn là **nhãn hiệu đã đăng ký** của ICC. Việc sao chép toàn văn quy tắc vào corpus và tái xuất bản qua chatbot có rủi ro vi phạm cả quyền tác giả lẫn quyền nhãn hiệu. |
| **Điều được phép (theo hiểu biết chung)** | Nêu **tên** 11 điều kiện · mô tả **nội dung nghiệp vụ** bằng ngôn ngữ của mình · lập bảng so sánh do mình tự soạn. Ý tưởng và sự kiện không được bảo hộ; cách diễn đạt cụ thể thì có. |
| **Phương án đã chọn** | **Tự biên soạn** ma trận trách nhiệm — [`12`](12-ma-tran-incoterms-2020.md). Toàn bộ diễn đạt do BA + chuyên gia viết, không sao chép câu chữ ICC. |
| **Nghĩa vụ kèm theo** | Cân nhắc ghi chú *"Nội dung diễn giải do đội ngũ biên soạn, không phải văn bản chính thức của ICC. Để tra cứu quy tắc chính thức, tham khảo ấn phẩm Incoterms® 2020 của ICC."* |
| **Kết luận Pháp chế** | *(S12 điền — cần trả lời: dùng chữ "Incoterms" trong giao diện sản phẩm thương mại có cần cấp phép nhãn hiệu không?)* |

### 2.2 UCP 600, ISBP 745, eUCP, URC 522 (ICC)

| | |
|---|---|
| **Phân tích của BA** | Cùng nhóm với 2.1. Đây là các bộ quy tắc thực hành thống nhất của ICC, có bản quyền. |
| **Phương án** | Nhóm I (thanh toán quốc tế) trong [`03`](03-pham-vi-nghiep-vu-v1.md) ở mức ưu tiên P2. Nếu làm, chỉ diễn giải nguyên tắc bằng ngôn ngữ của mình, không trích điều khoản UCP. |
| **Đề xuất cho v1** | **Hoãn nhóm I sang v2** để giảm bề mặt rủi ro pháp lý ở prototype. |
| **Kết luận Pháp chế** | *(S12 điền)* |

### 2.3 WCO Harmonized System Explanatory Notes

| | |
|---|---|
| Chủ sở hữu | World Customs Organization |
| **Phân tích của BA** | Chú giải chi tiết HS của WCO là ấn phẩm có bản quyền. **Không ingest.** |
| **Điều được phép** | Danh mục hàng hóa XNK Việt Nam do Bộ Tài chính ban hành là văn bản QPPL Việt Nam → dùng được toàn văn. Danh mục này đã bao gồm mã số, mô tả hàng hóa và các chú giải được nội luật hoá. |
| **Hệ quả thiết kế** | Bảng `danh_muc_hs` chỉ lấy từ Thông tư của Bộ Tài chính, **không** từ tài liệu WCO. |
| **Kết luận Pháp chế** | *(S12 điền)* |

### 2.4 Tiêu chuẩn, quy chuẩn kỹ thuật nước ngoài (ISO, IEC…)

| | |
|---|---|
| **Phân tích của BA** | Có bản quyền, thường phải mua. Chỉ liên quan gián tiếp qua nhóm G (kiểm tra chuyên ngành). |
| **Đề xuất** | Không ingest. Nếu cần, chỉ dẫn chiếu tên tiêu chuẩn và nơi mua. |
| **Kết luận Pháp chế** | *(S12 điền)* |

---

## 3. Nội dung do dự án tạo ra

| Nội dung | Ai viết | Ghi chú bản quyền |
|---|---|---|
| Ma trận Incoterms ([`12`](12-ma-tran-incoterms-2020.md)) | BA + chuyên gia | Tài sản của dự án |
| Từ điển thuật ngữ ([`11`](11-tu-dien-thuat-ngu-xnk.md)) | BA + chuyên gia | Tài sản của dự án |
| Golden set | Chuyên gia + BA | **Tài sản giá trị nhất** — cần được bảo vệ trong hợp đồng với chuyên gia thuê ngoài |
| System prompt | AI Engineer | Tài sản của dự án |
| Đặc tả quy trình BPMN | BA | Tài sản của dự án |

**⚠️ Điểm Pháp chế cần lưu ý:** nếu chuyên gia XNK là **cộng tác viên ngoài**, hợp đồng phải có điều khoản chuyển giao quyền sở hữu trí tuệ đối với golden set và nội dung nghiệp vụ họ tạo ra. Không có điều khoản này, tài sản giá trị nhất của dự án có tranh chấp sở hữu.

---

## 4. Nghĩa vụ khi thu thập dữ liệu (crawler)

| Vấn đề | Đề xuất của BA | Kết luận Pháp chế |
|---|---|---|
| `robots.txt` | Tuân thủ tuyệt đối | |
| Tần suất truy cập | ≤ 1 request / 2 giây / domain | |
| Định danh | `User-Agent` ghi rõ tên tổ chức + email liên hệ | |
| Điều khoản sử dụng của trang nguồn | **Đọc ToS của từng trang trước khi crawl** — kể cả trang nhà nước | |
| Lưu trữ file gốc | S3 + Object Lock, giữ `sha256` làm bằng chứng nội dung | |

*Prototype tải thủ công 15 văn bản nên chưa phát sinh vấn đề crawler. Mục này áp dụng từ Phase 1.*

---

## 5. Miễn trừ trách nhiệm

Đây là mục Pháp chế phải soạn, không phải BA. BA nêu yêu cầu chức năng:

| Yêu cầu | Vì sao |
|---|---|
| Disclaimer xuất hiện ở **cuối mỗi câu trả lời**, không chỉ ở trang điều khoản | Người dùng đọc câu trả lời, không đọc trang điều khoản |
| Nội dung nêu rõ: mang tính tham khảo, **không thay thế** tư vấn pháp lý chuyên môn hoặc quyết định của cơ quan có thẩm quyền | BR-10 |
| Riêng câu trả lời chứa mã HS: cảnh báo bổ sung về **thẩm quyền phân loại của cơ quan hải quan** | BR-04, guardrail ① |
| Riêng câu trả lời chứa thuế suất ưu đãi: nêu rõ **điều kiện áp dụng** | BR-05, guardrail ② |
| Ngắn gọn, không lấn át nội dung chính | Disclaimer dài quá thì người dùng bỏ qua |

**Đề xuất bản nháp để Pháp chế chỉnh sửa:**

> *Nội dung trên được tổng hợp từ văn bản quy phạm pháp luật hiện hành và chỉ mang tính tham khảo. Việc áp dụng vào trường hợp cụ thể thuộc trách nhiệm của người sử dụng và quyết định của cơ quan có thẩm quyền.*

| **Kết luận Pháp chế** | *(S12 soạn bản chính thức)* |
|---|---|

---

## 6. Bảo vệ dữ liệu cá nhân — chuyển tiếp cho DPO (S13)

Ngoài phạm vi bản quyền nhưng phải nêu để không bị bỏ sót:

| Vấn đề | Cần DPO trả lời |
|---|---|
| Lịch sử hội thoại có chứa dữ liệu cá nhân không? | |
| Câu hỏi của người dùng có thể chứa thông tin lô hàng, đối tác, giá cả — xử lý thế nào? | |
| Thời hạn lưu trữ lịch sử hội thoại | |
| Quyền xoá dữ liệu của người dùng | |
| Thoả thuận xử lý dữ liệu với tenant B2B | |
| Nghĩa vụ theo Nghị định 13/2023/NĐ-CP về bảo vệ dữ liệu cá nhân | |

---

## 7. Kết luận & điều kiện gỡ chặn

Tài liệu chuyển sang ✅ khi Pháp chế đã điền và ký toàn bộ ô "Kết luận Pháp chế" ở mục 1, 2.1–2.4, 4, 5.

**Sprint 1 chỉ được bắt đầu ingest dữ liệu khi:**

- [ ] Có kết luận cho mục 1 (văn bản QPPL Việt Nam) — cho phép ingest
- [ ] Có kết luận cho mục 2.1–2.3 — xác nhận **không** ingest tài liệu ICC/WCO
- [ ] Có bản disclaimer chính thức ở mục 5
- [ ] Hợp đồng với chuyên gia thuê ngoài có điều khoản sở hữu trí tuệ (nếu áp dụng)

---

## 8. Phê duyệt

| Vai trò | Người | Ngày | Chữ ký |
|---|---|---|---|
| BA (soạn phân tích) | | | |
| **Pháp chế (S12) — kết luận** | | | |
| DPO (S13) — mục 6 | | | |
| Product Owner | | | |
