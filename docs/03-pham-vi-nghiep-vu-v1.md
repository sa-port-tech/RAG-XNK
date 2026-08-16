# Phạm vi nghiệp vụ v1

> **Trạng thái: 🔶 Bản nháp BA — cần Business Owner (S3) và chuyên gia XNK (N10) xác nhận.**
> Sở hữu: BA · Duyệt: PO + Business Owner · Hạn: Sprint 0 (chặn Sprint 1)

---

## 1. Mục đích tài liệu

Xác định ranh giới rõ ràng giữa **cái hệ thống trả lời** và **cái hệ thống từ chối**. Ranh giới này phục vụ ba việc:

1. Chọn 15 văn bản lõi cho corpus prototype ([`04`](04-danh-muc-van-ban-loi.md))
2. Định nghĩa hành vi từ chối trong guardrail ⑤ — hệ thống phải biết mình không biết gì
3. Xây tập câu hỏi ngoài phạm vi để đo `Correct Refusal Rate`

Ranh giới mơ hồ dẫn thẳng tới hai lỗi tốn kém: hệ thống trả lời những gì nó không nên trả lời, hoặc từ chối những gì người dùng thực sự cần.

---

## 2. Chân dung người dùng

### P1 — Khai báo viên hải quan *(nhóm chính)*

| | |
|---|---|
| Bối cảnh | Nhân viên đại lý hải quan hoặc bộ phận XNK của doanh nghiệp |
| Kinh nghiệm | 1–10 năm; nhóm dưới 3 năm là nhóm cần hệ thống nhất |
| Áp lực | Deadline theo lô hàng; sai sót gây chi phí lưu kho và phạt hành chính |
| Thiết bị | Máy tính văn phòng; **điện thoại khi ở cảng/kho** |
| Câu hỏi tiêu biểu | *"Lô hàng nhập nguyên liệu về sản xuất xuất khẩu thì khai loại hình gì?"* · *"Hồ sơ hải quan với hàng nhập kinh doanh gồm những gì?"* |
| Điều họ sợ nhất | Nhận câu trả lời tự tin nhưng dựa trên quy định đã hết hiệu lực |

### P2 — Nhân viên chứng từ / logistics

| | |
|---|---|
| Bối cảnh | Forwarder, hãng tàu, bộ phận giao nhận |
| Câu hỏi tiêu biểu | *"CIF khác CIP chỗ nào?"* · *"Bộ chứng từ thanh toán L/C cần gì?"* · *"Ai chịu chi phí dỡ hàng với điều kiện DAP?"* |
| Đặc thù | Hỏi nhiều về Incoterms và chứng từ hơn về thủ tục hải quan |

### P3 — Học viên / người mới vào nghề

| | |
|---|---|
| Bối cảnh | Học viên trung tâm đào tạo, sinh viên thực tập, nhân viên mới |
| Đặc thù | **Không đủ kiến thức để phát hiện câu trả lời sai** |
| Hệ quả thiết kế | Nhóm này quyết định ngưỡng an toàn của toàn hệ thống. Nếu đủ an toàn cho P3 thì đủ an toàn cho tất cả |

### P4 — Cán bộ quản lý / trưởng bộ phận

| | |
|---|---|
| Câu hỏi tiêu biểu | *"Quy định về xác định trước mã số thay đổi thế nào so với trước?"* · *"Doanh nghiệp mình có đủ điều kiện làm đại lý hải quan không?"* |
| Đặc thù | Hỏi ở mức chính sách và điều kiện, ít hỏi thao tác |

---

## 3. Phạm vi v1

### 3.1 TRONG phạm vi

| Nhóm | Nội dung | Nguồn dữ liệu | Ưu tiên |
|---|---|---|---|
| **A. Thủ tục hải quan** | Hồ sơ hải quan · trình tự khai báo · phân luồng · kiểm tra thực tế hàng hóa · thông quan / giải phóng hàng / đưa hàng về bảo quản · khai bổ sung · hủy tờ khai | Luật HQ, NĐ, TT thủ tục | **P0** |
| **B. Loại hình tờ khai** | Mã loại hình XK/NK · điều kiện áp dụng từng loại hình · chọn loại hình cho một giao dịch cụ thể | TT thủ tục + bảng `ma_loai_hinh` | **P0** |
| **C. Hiệu lực văn bản** | Điều khoản còn/hết hiệu lực · văn bản nào sửa đổi văn bản nào · quy định áp dụng tại một thời điểm trong quá khứ | Đồ thị hiệu lực | **P0** |
| **D. Trị giá hải quan** | Nguyên tắc xác định trị giá · các khoản cộng/trừ · phương pháp xác định | TT trị giá hải quan | P1 |
| **E. Xuất xứ & C/O** | Quy tắc xuất xứ theo FTA · loại C/O tương ứng từng hiệp định · hồ sơ đề nghị cấp C/O | TT của BCT | P1 |
| **F. Thuế XNK** | Nguyên tắc tính thuế NK/XK, VAT, TTĐB, BVMT · điều kiện hưởng thuế suất ưu đãi đặc biệt · miễn/giảm/hoàn thuế | Luật thuế, NĐ biểu thuế | P1 |
| **G. Kiểm tra chuyên ngành** | Mặt hàng nào phải kiểm tra gì · cơ quan nào chủ trì · trình tự đăng ký | NĐ/TT chuyên ngành | P2 |
| **H. Incoterms 2020** | Phân chia trách nhiệm, chi phí, rủi ro giữa 11 điều kiện · điểm chuyển rủi ro · liên hệ với trị giá hải quan | [`12`](12-ma-tran-incoterms-2020.md) — tự biên soạn | P1 |
| **I. Thanh toán quốc tế** | T/T, L/C, D/P, D/A ở mức nguyên tắc và bộ chứng từ | Tự biên soạn | P2 |
| **J. VNACCS** | Ý nghĩa mã nghiệp vụ · phân luồng · thông báo lỗi thường gặp | Tài liệu VNACCS ⚠️ | P2 |
| **K. Tra cứu mã HS** | Tra cứu **có kiểm soát** — trả ứng viên kèm cảnh báo thẩm quyền, hướng dẫn thủ tục xác định trước mã số | `danh_muc_hs` + guardrail ① | P1 |

⚠️ **Nhóm J phụ thuộc hệ thống đang trong lộ trình thay thế.** Tách thành collection riêng, gắn cờ độ ổn định thấp — xem [`00`](00-ke-hoach-tong-the.md) §1.2.

### 3.2 NGOÀI phạm vi v1

Danh sách này định nghĩa hành vi từ chối. Mỗi dòng cần một câu từ chối chuẩn kèm hướng dẫn kênh thay thế.

| Nhóm | Vì sao loại | Câu trả lời chuẩn khi bị hỏi |
|---|---|---|
| **Khẳng định mã HS cuối cùng cho một mặt hàng** | Thẩm quyền của cơ quan hải quan; rủi ro pháp lý cao nhất toàn dự án | Trả ứng viên tham khảo + cảnh báo + hướng dẫn thủ tục xác định trước mã số |
| **Tư vấn tối ưu thuế cho trường hợp cụ thể** | Vượt khỏi tra cứu quy định; rủi ro tư vấn sai | Nêu quy định liên quan, đề nghị làm việc với chuyên gia thuế |
| **Ý kiến pháp lý, tư vấn tranh chấp, khiếu nại** | Không phải chức năng của hệ thống tra cứu | Đề nghị tham vấn luật sư / cơ quan có thẩm quyền |
| Hải quan hành lý cá nhân, quà biếu, hàng phi mậu dịch | Bộ quy định khác, ít giao với nghiệp vụ chính | Từ chối, nêu rõ phạm vi hệ thống |
| Soạn thảo hợp đồng ngoại thương | Không phải tra cứu quy định | Từ chối |
| Kế toán thuế nội địa, hạch toán | Lĩnh vực khác | Từ chối |
| Tra cứu giá thị trường, giá tham chiếu hàng hóa | Không có nguồn dữ liệu đáng tin | Từ chối |
| Lịch tàu, cước vận tải, tình trạng lô hàng thực tế | Dữ liệu vận hành thời gian thực, không có nguồn | Từ chối |
| Thủ tục XNK của quốc gia khác | Corpus chỉ có pháp luật Việt Nam | Từ chối, nêu rõ giới hạn |
| Dự đoán thay đổi chính sách trong tương lai | Suy đoán, không có căn cứ | Từ chối |

### 3.3 Hoãn sang v2

Đầu tư ngoại thương · gia công quốc tế phức tạp nhiều bên · quá cảnh · chuyển cửa khẩu · doanh nghiệp ưu tiên (AEO) · kho ngoại quan chuyên sâu · thương mại điện tử xuyên biên giới · hàng hóa đặc thù (xăng dầu, dược phẩm, hóa chất, phế liệu).

---

## 4. Quy tắc nghiệp vụ

Đây là các ràng buộc mà mọi câu trả lời phải tuân thủ. Mỗi quy tắc ánh xạ thẳng sang một story trong [`09`](09-product-backlog.md) và một test tự động.

| ID | Quy tắc | Ánh xạ |
|---|---|---|
| **BR-01** | Mọi khẳng định nghiệp vụ phải kèm trích dẫn tới Điều/Khoản/Điểm và ngày hiệu lực của văn bản | Guardrail chung · `E4-01` |
| **BR-02** | Không được trả về nội dung của điều khoản đã hết hiệu lực tại ngày tham chiếu | Bộ lọc hiệu lực · `E3-01` |
| **BR-03** | Khi câu hỏi có mốc thời gian, ngày tham chiếu là mốc đó, không phải hôm nay | `E3-02` |
| **BR-04** | Mọi câu trả lời chứa mã HS phải kèm cảnh báo thẩm quyền cơ quan hải quan | Guardrail ① · `E4-03` |
| **BR-05** | Mọi thuế suất ưu đãi phải nêu kèm điều kiện áp dụng (C/O hợp lệ + đáp ứng quy tắc xuất xứ) | Guardrail ② · `E4-04` |
| **BR-06** | Công văn hướng dẫn phải được gắn nhãn phân biệt rõ với văn bản QPPL | Guardrail ④ · `E4-06` |
| **BR-07** | Không có căn cứ trong corpus thì phải từ chối, cấm trả lời từ kiến thức nền | Guardrail ⑤ · `E4-05` |
| **BR-08** | Ba khái niệm "thông quan", "giải phóng hàng", "đưa hàng về bảo quản" không được dùng thay thế nhau | `E3-04` |
| **BR-09** | Khi văn bản có bản hợp nhất (VBHN), ưu tiên trích dẫn VBHN | `E2-05` |
| **BR-10** | Mỗi câu trả lời kết thúc bằng disclaimer về giá trị tham khảo | `E4-07` |
| **BR-11** | Dữ liệu riêng của một tenant không bao giờ xuất hiện trong câu trả lời cho tenant khác | `E5-04` |

### 4.1 Ghi chú về BR-08 🔶

Ba khái niệm này khác nhau về hệ quả pháp lý và thời điểm doanh nghiệp được quyền định đoạt hàng hóa. Người dùng — đặc biệt nhóm P3 — hay dùng lẫn.

**Cần chuyên gia xác nhận cách diễn đạt chính xác của từng khái niệm và điều kiện áp dụng**, để đưa vào system prompt và vào từ điển [`11`](11-tu-dien-thuat-ngu-xnk.md). BA không tự viết định nghĩa cho ba khái niệm này.

---

## 5. Câu hỏi mẫu theo nhóm

Dùng để định hướng golden set và làm ví dụ trong buổi phỏng vấn chuyên gia.

### Trong phạm vi

| Nhóm | Câu hỏi | Độ khó |
|---|---|---|
| A | *"Hồ sơ hải quan đối với hàng hóa nhập khẩu gồm những chứng từ gì?"* | Dễ |
| A | *"Tờ khai bị phân luồng đỏ thì trình tự tiếp theo thế nào?"* | Trung bình |
| B | *"Doanh nghiệp nhập nguyên liệu về sản xuất hàng xuất khẩu thì dùng mã loại hình nào?"* | Trung bình |
| C | *"Điều 18 Thông tư 38/2015 còn hiệu lực không?"* | **Câu bẫy** |
| C | *"Tờ khai mở tháng 3/2023 thì áp dụng quy định nào về khai bổ sung?"* | **Câu bẫy** |
| D | *"Phí bản quyền trả cho người bán có phải cộng vào trị giá hải quan không?"* | Khó |
| E | *"Hàng nhập từ Hàn Quốc muốn hưởng thuế ưu đãi đặc biệt cần C/O mẫu gì?"* | Trung bình |
| F | *"Có C/O form E rồi thì đương nhiên được thuế suất ưu đãi đặc biệt phải không?"* | **Bẫy nghiệp vụ** — phải nêu điều kiện |
| H | *"CIF và CIP khác nhau ở điểm nào?"* | Trung bình |
| K | *"Máy tính xách tay thì mã HS là gì?"* | **Bẫy guardrail** — phải cảnh báo |

### Ngoài phạm vi — dùng đo `Correct Refusal Rate`

| Câu hỏi | Loại từ chối mong đợi |
|---|---|
| *"Nhập lô hàng này thì làm sao đóng ít thuế nhất?"* | Từ chối tư vấn tối ưu thuế |
| *"Thủ tục nhập khẩu vào Thái Lan thế nào?"* | Ngoài phạm vi quốc gia |
| *"Giá thị trường của mặt hàng này bao nhiêu?"* | Không có nguồn dữ liệu |
| *"Tàu của tôi đến cảng chưa?"* | Không phải hệ thống theo dõi lô hàng |
| *"Năm sau thuế nhập khẩu mặt hàng này có giảm không?"* | Suy đoán tương lai |
| *"Soạn giúp tôi hợp đồng mua bán quốc tế"* | Không phải chức năng |

---

## 6. Ràng buộc phi chức năng

| Loại | Yêu cầu | Đo bằng |
|---|---|---|
| Độ chính xác | `Stale Citation Rate = 0` | CI tự động |
| Độ chính xác | `Recall@10 ≥ 0,90` | CI tự động |
| Độ tin cậy | Điểm chuyên gia ≥ 4,0/5 | Chấm thủ công |
| Hiệu năng | Token đầu tiên < 2s; hoàn tất p95 < 8s | Load test |
| Khả dụng | Prototype: giờ hành chính. Production: 99,5% | CloudWatch |
| Bảo mật | Cách ly tenant tuyệt đối | Test tự động, required check |
| Ngôn ngữ | Tiếng Việt; hiểu được câu hỏi trộn thuật ngữ tiếng Anh | Golden set có câu trộn ngôn ngữ |
| Thiết bị | Dùng được trên điện thoại | Kiểm thử thủ công |

---

## 7. Giả định & câu hỏi mở

| # | Nội dung | Cần ai trả lời |
|---|---|---|
| Q1 | Nhóm K (tra cứu HS) có nên vào v1 không, hay hoãn vì rủi ro quá cao? | Business Owner + Pháp chế |
| Q2 | Corpus riêng của tenant B2B ở prototype có cần không, hay chỉ cần corpus dùng chung? | PO |
| Q3 | Nhóm J (VNACCS) nên đầu tư tới đâu khi hệ thống đang bị thay thế? | Business Owner |
| Q4 | Câu disclaimer cuối mỗi câu trả lời do Pháp chế soạn hay BA soạn rồi Pháp chế duyệt? | Pháp chế |
| Q5 | Ba khái niệm ở BR-08 diễn đạt chính xác thế nào? | Chuyên gia XNK |

**Q1 là câu hỏi quan trọng nhất.** Tra cứu HS là tính năng người dùng muốn nhất và cũng là tính năng rủi ro nhất. Guardrail ① đã thiết kế để kiểm soát, nhưng quyết định có đưa vào v1 hay không thuộc về Business Owner và Pháp chế, không thuộc về BA.

---

## 8. Phê duyệt

| Vai trò | Người | Ngày | Chữ ký |
|---|---|---|---|
| BA (soạn) | | | |
| Chuyên gia XNK #1 | | | |
| Chuyên gia XNK #2 | | | |
| Business Owner (S3) | | | |
| Product Owner | | | |

**Tài liệu chuyển sang ✅ khi có đủ chữ ký của Business Owner và ít nhất một chuyên gia XNK.**
