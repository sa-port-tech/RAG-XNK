# Từ điển thuật ngữ & viết tắt XNK

> **Trạng thái: 🔶 Bản nháp BA — cần chuyên gia XNK rà soát toàn bộ, đặc biệt mục §6 (cặp thuật ngữ dễ nhầm).**
> Sở hữu: BA + Chuyên gia XNK · Mục tiêu ~300 mục · Hiện có: 112 mục

---

## 1. Mục đích

Từ điển này phục vụ **bốn** việc kỹ thuật, không phải để tra cứu cho vui:

| Dùng ở đâu | Vai trò |
|---|---|
| **Query expansion** (E3-10) | Người dùng gõ `TK`, hệ thống phải hiểu là "tờ khai" |
| **System prompt** (E4-12) | Model cần biết thuật ngữ để không diễn giải sai |
| **Nhận diện thực thể** (E3-07) | Regex bắt mã loại hình, mã nghiệp vụ |
| **Onboarding người mới** | Đọc trước khi họp với chuyên gia |

## 2. Cảnh báo về query expansion ⚠️

**Không phải cặp từ nào cũng được gộp trong query expansion.** Ba khái niệm ở §6 có hệ quả pháp lý khác nhau; gộp chúng lại làm hệ thống trả lời sai. Cột `gop_query` trong file dữ liệu đánh dấu mục nào được mở rộng, mục nào cấm.

File dữ liệu: `prompts/tu-dien-xnk.json` — versioned trong git, thay đổi cần approval của `@xnk-expert-team`.

---

## 3. Viết tắt tiếng Việt

| Viết tắt | Đầy đủ | Ghi chú |
|---|---|---|
| XNK | Xuất nhập khẩu | |
| XK / NK | Xuất khẩu / Nhập khẩu | |
| TK | Tờ khai | Trong ngữ cảnh HQ là tờ khai hải quan |
| TKHQ | Tờ khai hải quan | |
| HQ | Hải quan | |
| KTCN | Kiểm tra chuyên ngành | |
| KDTV | Kiểm dịch thực vật | |
| KDĐV | Kiểm dịch động vật | |
| ATTP | An toàn thực phẩm | |
| SXXK | Sản xuất xuất khẩu | Loại hình nhập nguyên liệu về SX hàng XK |
| GC | Gia công | |
| TNTX | Tạm nhập tái xuất | |
| TXTN | Tạm xuất tái nhập | |
| KNQ | Kho ngoại quan | |
| DNCX | Doanh nghiệp chế xuất | |
| KCX / KCN | Khu chế xuất / Khu công nghiệp | |
| ĐLHQ | Đại lý hải quan | |
| GTGT | Giá trị gia tăng | Thuế VAT |
| TTĐB | Tiêu thụ đặc biệt | |
| BVMT | Bảo vệ môi trường | Thuế BVMT |
| CBPG | Chống bán phá giá | |
| CTC | Chống trợ cấp | |
| QPPL | Quy phạm pháp luật | |
| VBHN | Văn bản hợp nhất | Quan trọng với BR-09 |
| NĐ / TT / QĐ / CV | Nghị định / Thông tư / Quyết định / Công văn | |
| BTC / BCT | Bộ Tài chính / Bộ Công Thương | ⚠️ xem §7 về tái cơ cấu 2025 |
| CQHQ | Cơ quan hải quan | |
| NNK / NXK | Người nhập khẩu / Người xuất khẩu | |
| DN | Doanh nghiệp | |
| GPNK / GPXK | Giấy phép nhập khẩu / xuất khẩu | |
| CBHQ | Công chức hải quan | |

## 4. Viết tắt tiếng Anh — chứng từ & thương mại

| Viết tắt | Đầy đủ | Tiếng Việt |
|---|---|---|
| C/O | Certificate of Origin | Giấy chứng nhận xuất xứ hàng hóa |
| C/Q | Certificate of Quality | Giấy chứng nhận chất lượng |
| C/I | Commercial Invoice | Hóa đơn thương mại |
| P/L | Packing List | Phiếu đóng gói hàng hóa |
| B/L | Bill of Lading | Vận đơn đường biển |
| HBL / MBL | House / Master Bill of Lading | Vận đơn nhà / vận đơn chủ |
| AWB | Air Waybill | Vận đơn hàng không |
| D/O | Delivery Order | Lệnh giao hàng |
| S/I | Shipping Instruction | Hướng dẫn giao hàng |
| B/K | Booking Note | Lệnh đặt chỗ |
| PO | Purchase Order | Đơn đặt hàng |
| P/I | Proforma Invoice | Hóa đơn chiếu lệ |
| INS | Insurance Certificate | Chứng thư bảo hiểm |
| PHYTO | Phytosanitary Certificate | Giấy chứng nhận kiểm dịch thực vật |
| HS | Harmonized System | Hệ thống hài hòa mô tả và mã hóa hàng hóa |
| AHTN | ASEAN Harmonised Tariff Nomenclature | Danh mục thuế quan hài hòa ASEAN |

## 5. Viết tắt tiếng Anh — thanh toán, vận tải, hệ thống

| Viết tắt | Đầy đủ | Tiếng Việt |
|---|---|---|
| L/C | Letter of Credit | Thư tín dụng |
| T/T | Telegraphic Transfer | Chuyển tiền bằng điện |
| D/P | Documents against Payment | Nhờ thu trả tiền ngay |
| D/A | Documents against Acceptance | Nhờ thu trả chậm |
| FCL / LCL | Full / Less than Container Load | Hàng nguyên container / hàng lẻ |
| CY / CFS | Container Yard / Container Freight Station | Bãi container / Địa điểm thu gom hàng lẻ |
| ICD | Inland Container Depot | Cảng cạn |
| THC | Terminal Handling Charge | Phí xếp dỡ tại cảng |
| DEM / DET | Demurrage / Detention | Phí lưu container tại bãi / tại kho |
| ETA / ETD | Estimated Time of Arrival / Departure | Thời gian đến / đi dự kiến |
| VNACCS | Vietnam Automated Cargo Clearance System | Hệ thống thông quan tự động ⚠️ §8 |
| VCIS | Vietnam Customs Intelligence System | Hệ thống thông tin tình báo hải quan ⚠️ §8 |
| NSW | National Single Window | Cơ chế một cửa quốc gia |
| ASW | ASEAN Single Window | Cơ chế một cửa ASEAN |
| AEO | Authorized Economic Operator | Doanh nghiệp ưu tiên |
| MFN | Most Favoured Nation | Đối xử tối huệ quốc |
| WCO / WTO | World Customs / Trade Organization | Tổ chức Hải quan / Thương mại Thế giới |
| ICC | International Chamber of Commerce | Phòng Thương mại Quốc tế |

## 6. ⚠️ Cặp thuật ngữ dễ nhầm — CẤM gộp trong query expansion 🔶

**Đây là mục quan trọng nhất của tài liệu, và là mục BA không tự viết định nghĩa.**

Ba khái niệm dưới đây khác nhau về **hệ quả pháp lý** và **thời điểm doanh nghiệp được quyền định đoạt hàng hóa**. Người dùng — đặc biệt nhóm P3 — thường dùng lẫn. Nếu hệ thống gộp chúng, nó trả lời sai một cách rất khó phát hiện.

| Khái niệm | Định nghĩa | Điều kiện áp dụng | Hệ quả |
|---|---|---|---|
| **Thông quan** | 🔶 *(chuyên gia điền)* | 🔶 | 🔶 |
| **Giải phóng hàng** | 🔶 *(chuyên gia điền)* | 🔶 | 🔶 |
| **Đưa hàng về bảo quản** | 🔶 *(chuyên gia điền)* | 🔶 | 🔶 |

**Yêu cầu hành vi (BR-08):** khi phát hiện người dùng dùng lẫn ba khái niệm này, hệ thống **làm rõ sự khác biệt trong câu trả lời** thay vì trả lời theo cách hiểu của người hỏi.

### Các cặp khác cần chuyên gia rà 🔶

| Cặp | Vì sao dễ nhầm |
|---|---|
| Kiểm tra hồ sơ ↔ Kiểm tra thực tế hàng hóa | Hai bước khác nhau, "kiểm hóa" thường chỉ bước sau |
| Trị giá hải quan ↔ Trị giá giao dịch | Không đồng nhất |
| Xuất xứ ↔ Nơi sản xuất ↔ Nơi giao hàng | Ba khái niệm khác nhau, ảnh hưởng thuế suất ưu đãi |
| Thuế suất ưu đãi ↔ Thuế suất ưu đãi đặc biệt ↔ Thuế suất thông thường | Ba mức khác nhau, điều kiện khác nhau |
| Miễn thuế ↔ Không chịu thuế ↔ Hoàn thuế | Ba cơ chế khác nhau |
| Hủy tờ khai ↔ Khai bổ sung ↔ Sửa tờ khai | Ba thủ tục khác nhau |
| Đại lý hải quan ↔ Người khai hải quan | Tư cách pháp lý khác nhau |

---

## 7. ⚠️ Tên cơ quan sau tái cơ cấu 2025 🔶

Văn bản ban hành trước và sau tái cơ cấu dùng tên cơ quan khác nhau. Hệ thống cần hiển thị đúng tên **tại thời điểm ban hành**, đồng thời hiểu được khi người dùng gọi bằng tên cũ.

| Tên cũ | Tên hiện hành | Hiệu lực từ |
|---|---|---|
| 🔶 *(chuyên gia điền — tra từ văn bản tổ chức bộ máy)* | | |

**BA không suy luận tên cơ quan sau tái cơ cấu.** Đây là thông tin phải tra từ văn bản chính thức. Bảng này dùng chung với [`04`](04-danh-muc-van-ban-loi.md) §6.

---

## 8. ⚠️ Mã nghiệp vụ VNACCS 🔶

**Cảnh báo:** VNACCS/VCIS đang trong lộ trình thay thế bởi hệ thống CNTT hải quan mới. Nhóm mục này tách riêng, gắn cờ độ ổn định thấp, cập nhật độc lập với phần còn lại của từ điển.

| Mã | Nghiệp vụ | Xác minh |
|---|---|---|
| IDA | Khai thông tin nhập khẩu (bản nháp) | ⬜ |
| IDC | Đăng ký tờ khai nhập khẩu | ⬜ |
| EDA | Khai thông tin xuất khẩu (bản nháp) | ⬜ |
| EDC | Đăng ký tờ khai xuất khẩu | ⬜ |
| MIC | Khai tờ khai nhập khẩu trị giá thấp | ⬜ |
| MEC | Khai tờ khai xuất khẩu trị giá thấp | ⬜ |
| IDA01 | Khai bổ sung tờ khai nhập khẩu | ⬜ |
| IDB | Gọi thông tin tờ khai nhập khẩu | ⬜ |

**Phân luồng tờ khai** 🔶

| Luồng | Ý nghĩa dự kiến — *cần chuyên gia xác nhận* | Xác minh |
|---|---|---|
| Xanh | Miễn kiểm tra hồ sơ, miễn kiểm tra thực tế | ⬜ |
| Vàng | Kiểm tra hồ sơ | ⬜ |
| Đỏ | Kiểm tra hồ sơ và kiểm tra thực tế hàng hóa | ⬜ |

**Toàn bộ mã ở §8 phải được chuyên gia đối chiếu với tài liệu hướng dẫn hiện hành.** BA liệt kê theo hiểu biết chung để mở đầu buổi làm việc, không phải để dùng làm căn cứ.

---

## 9. Hiệp định thương mại tự do (FTA)

| Viết tắt | Tên đầy đủ |
|---|---|
| ATIGA | Hiệp định Thương mại Hàng hóa ASEAN |
| ACFTA | Khu vực mậu dịch tự do ASEAN – Trung Quốc |
| AKFTA / VKFTA | ASEAN – Hàn Quốc / Việt Nam – Hàn Quốc |
| AJCEP / VJEPA | ASEAN – Nhật Bản / Việt Nam – Nhật Bản |
| AANZFTA | ASEAN – Australia – New Zealand |
| AIFTA | ASEAN – Ấn Độ |
| AHKFTA | ASEAN – Hồng Kông |
| EVFTA | Việt Nam – Liên minh châu Âu |
| UKVFTA | Việt Nam – Vương quốc Anh |
| CPTPP | Đối tác Toàn diện và Tiến bộ xuyên Thái Bình Dương |
| RCEP | Đối tác Kinh tế Toàn diện Khu vực |
| VCFTA / VN-EAEU | Việt Nam – Chile / Việt Nam – Liên minh Kinh tế Á Âu |

**⚠️ Mẫu C/O tương ứng từng hiệp định 🔶** — chuyên gia điền, BA không tự ánh xạ vì sai một mẫu là hỏng cả câu trả lời về thuế ưu đãi.

| FTA | Mẫu C/O | Xác minh |
|---|---|---|
| 🔶 *(chuyên gia điền)* | | ⬜ |

---

## 10. Kế hoạch hoàn thiện

| Giai đoạn | Mục tiêu | Ai |
|---|---|---|
| Sprint 0 | 112 mục hiện có được chuyên gia rà, đánh dấu đúng/sai/sửa | BA + Chuyên gia |
| Sprint 0 | **Hoàn thành §6** — ba khái niệm và các cặp dễ nhầm | Chuyên gia |
| Sprint 0 | Hoàn thành §7 — bảng tên cơ quan | Chuyên gia |
| Sprint 1 | Bổ sung lên ~200 mục từ 15 văn bản lõi | BA |
| Sprint 1 | Xác minh §8 — mã VNACCS | Chuyên gia |
| Sprint 2 | Bổ sung lên ~300 mục từ golden set và câu hỏi thật | BA |
| Phase 1 | Mở rộng liên tục từ câu hỏi người dùng | BA |

**Ưu tiên cao nhất là §6 và §7**, không phải số lượng mục. Một từ điển 300 mục mà gộp nhầm "thông quan" với "giải phóng hàng" còn tệ hơn từ điển 50 mục làm đúng.

---

## 11. Phê duyệt

| Vai trò | Người | Ngày | Chữ ký |
|---|---|---|---|
| BA (soạn) | | | |
| Chuyên gia XNK #1 | | | |
| Chuyên gia XNK #2 | | | |
| AI Engineer (xác nhận dùng được cho query expansion) | | | |
