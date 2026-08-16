# Danh mục văn bản lõi — Corpus prototype

> **Trạng thái: 🔶 Bản nháp BA — TOÀN BỘ số hiệu, ngày hiệu lực và trạng thái trong tài liệu này PHẢI được chuyên gia XNK xác minh trực tiếp trên `vbpl.vn` trước khi dùng.**
> Sở hữu: BA (cấu trúc) + Chuyên gia XNK (nội dung) · Hạn: Sprint 0 (chặn Sprint 1)

---

## ⚠️ Cảnh báo bắt buộc đọc

**BA không có thẩm quyền xác nhận số hiệu văn bản, ngày hiệu lực hay trạng thái hiệu lực.** Danh sách dưới đây là **gợi ý khởi điểm** dựa trên hiểu biết chung về hệ thống văn bản XNK Việt Nam, phục vụ việc định hướng buổi làm việc với chuyên gia.

Mỗi dòng phải trải qua ba bước trước khi được ✅:

1. Chuyên gia tra trên `vbpl.vn` → xác nhận số hiệu chính xác
2. Ghi lại **ngày ban hành, ngày hiệu lực, trạng thái hiệu lực hiện tại** từ nguồn chính thức
3. Ghi lại **danh sách văn bản sửa đổi/thay thế** và **văn bản hợp nhất (VBHN)** nếu có

Việc bỏ qua ba bước này làm sai toàn bộ đồ thị hiệu lực — và sai đồ thị hiệu lực là sai vĩnh viễn mọi câu trả lời của hệ thống.

---

## 1. Tiêu chí chọn 15 văn bản

| Tiêu chí | Vì sao |
|---|---|
| **Có ít nhất một cặp sửa đổi một phần** | Chứng minh luận điểm kiến trúc quan trọng nhất: văn bản "còn hiệu lực" nhưng nhiều Điều bên trong đã bị thay thế |
| Phủ được nhóm A, B, C trong [`03`](03-pham-vi-nghiep-vu-v1.md) §3.1 | Ba nhóm P0 |
| Có sẵn bản text layer, không cần OCR | OCR là spike riêng, không được chặn đường tới kết luận |
| Đủ dài để retrieval có ý nghĩa (≥ 50 Điều tổng cộng) | Corpus quá nhỏ thì Recall@10 không phản ánh thực tế |
| Là văn bản khai báo viên tra cứu hàng ngày | Golden set phải là câu hỏi thật |

---

## 2. Danh mục đề xuất 🔶

### Nhóm A — Trục thủ tục hải quan *(bắt buộc)*

| # | Loại | Gợi ý văn bản | Vai trò trong corpus | Xác minh |
|---|---|---|---|---|
| 1 | Luật | Luật Hải quan | Nền tảng: khái niệm, nguyên tắc, thẩm quyền | ⬜ |
| 2 | Nghị định | NĐ quy định chi tiết thi hành Luật Hải quan về thủ tục, kiểm tra, giám sát | Chi tiết hoá Luật | ⬜ |
| 3 | Nghị định | NĐ sửa đổi, bổ sung NĐ ở dòng 2 | **Cặp sửa đổi thứ nhất** | ⬜ |
| 4 | Thông tư | TT quy định thủ tục hải quan, kiểm tra giám sát hải quan, thuế XNK và quản lý thuế | **Văn bản khai báo viên dùng nhiều nhất** | ⬜ |
| 5 | Thông tư | TT sửa đổi, bổ sung TT ở dòng 4 | **Cặp sửa đổi thứ hai — ca kiểm chứng chính** | ⬜ |
| 6 | VBHN | Văn bản hợp nhất của cặp 4–5 *(nếu có)* | Kiểm chứng quy tắc BR-09 | ⬜ |

> **Cặp 4–5 là lý do tồn tại của corpus prototype.** Một Thông tư sửa đổi một phần Thông tư khác — chính xác ca mà bộ lọc hiệu lực cấp văn bản sẽ bỏ sót. Nếu chuyên gia xác nhận được cặp này, ta có ngay 10 câu bẫy.

### Nhóm B — Loại hình & trị giá

| # | Loại | Gợi ý văn bản | Vai trò | Xác minh |
|---|---|---|---|---|
| 7 | Thông tư | TT về trị giá hải quan hàng hóa XNK | Nhóm D | ⬜ |
| 8 | Thông tư | TT sửa đổi TT ở dòng 7 | Cặp sửa đổi thứ ba | ⬜ |
| 9 | Phụ lục | Bảng mã loại hình tờ khai XK/NK | Nguồn cho bảng `ma_loai_hinh` | ⬜ |

### Nhóm C — Thuế & xuất xứ

| # | Loại | Gợi ý văn bản | Vai trò | Xác minh |
|---|---|---|---|---|
| 10 | Luật | Luật Thuế xuất khẩu, thuế nhập khẩu | Nhóm F | ⬜ |
| 11 | Nghị định | NĐ quy định về miễn, giảm, hoàn thuế XNK | Nhóm F | ⬜ |
| 12 | Nghị định | NĐ sửa đổi NĐ ở dòng 11 | Cặp sửa đổi thứ tư | ⬜ |
| 13 | Thông tư | TT của BCT về quy tắc xuất xứ và C/O cho **một** FTA *(chọn hiệp định có lưu lượng lớn nhất)* | Nhóm E | ⬜ |

### Nhóm D — Quản lý ngoại thương

| # | Loại | Gợi ý văn bản | Vai trò | Xác minh |
|---|---|---|---|---|
| 14 | Luật | Luật Quản lý ngoại thương | Khung pháp lý XNK | ⬜ |
| 15 | Nghị định | NĐ quy định chi tiết Luật Quản lý ngoại thương | Danh mục hàng cấm/hạn chế, giấy phép | ⬜ |

### Bổ sung khuyến nghị (ngoài 15) 🔶

| Loại | Nội dung | Vì sao nên có |
|---|---|---|
| Công văn hướng dẫn | 2–3 công văn của cơ quan hải quan về tình huống thường gặp | **Kiểm chứng guardrail ④** — công văn không phải văn bản QPPL. Không có công văn nào trong corpus thì không test được quy tắc này |

---

## 3. Bảng metadata phải thu thập cho từng văn bản

Chuyên gia điền bảng này cho mỗi dòng ở §2. Đây là đầu vào trực tiếp của schema `van_ban`.

| Trường | Nguồn | Bắt buộc |
|---|---|---|
| `so_hieu` | vbpl.vn | ✅ |
| `loai_van_ban` | vbpl.vn | ✅ |
| `trich_yeu` | vbpl.vn | ✅ |
| `co_quan_ban_hanh` | vbpl.vn — ⚠️ lưu ý tái cơ cấu bộ máy 2025 | ✅ |
| `nguoi_ky` | vbpl.vn | |
| `ngay_ban_hanh` | vbpl.vn | ✅ |
| `ngay_hieu_luc` | vbpl.vn | ✅ |
| `ngay_het_hieu_luc` | vbpl.vn | nếu có |
| `trang_thai` | vbpl.vn | ✅ |
| `van_ban_sua_doi[]` | vbpl.vn — mục "văn bản liên quan" | ✅ |
| `van_ban_bi_sua_doi[]` | vbpl.vn | ✅ |
| `co_vbhn` | vbpl.vn | ✅ |
| `nguon_url` | | ✅ |
| `dinh_dang_file` | PDF text layer / PDF scan / DOCX | ✅ |
| `so_dieu_uoc_tinh` | Đếm thủ công | ✅ |

---

## 4. Bảng quan hệ sửa đổi — đầu vào của đồ thị hiệu lực

**Đây là phần quan trọng nhất của tài liệu này.** Với mỗi cặp sửa đổi, chuyên gia phải liệt kê **chính xác từng Điều/Khoản bị tác động**, không được ghi chung chung "sửa đổi nhiều điều".

| VB sửa đổi | VB bị sửa đổi | Loại | Điều/Khoản bị tác động | Ngày hiệu lực của sửa đổi | Xác minh |
|---|---|---|---|---|---|
| *(điền)* | *(điền)* | sửa_đổi / bổ_sung / bãi_bỏ / thay_thế | *(liệt kê từng Điều)* | *(điền)* | ⬜ |

**Cách làm:** mở văn bản sửa đổi, đọc từng khoản dạng *"Sửa đổi, bổ sung Điều X…"*, *"Bãi bỏ Khoản Y Điều Z…"*, ghi lại. Không suy đoán, không tóm tắt.

Kết quả bảng này nạp thẳng vào bảng `sua_doi` và là căn cứ để Data Engineer viết test cho hàm `hieu_luc_tai_ngay()`.

---

## 5. Rủi ro của tài liệu này

| Rủi ro | Hệ quả | Giảm thiểu |
|---|---|---|
| Chuyên gia điền vội, ghi "sửa đổi nhiều điều" thay vì liệt kê | Đồ thị hiệu lực sai → toàn hệ thống sai | BA ngồi cùng khi điền, không gửi form đi rồi chờ |
| Lấy số hiệu từ Google thay vì `vbpl.vn` | Trạng thái hiệu lực có thể lỗi thời | Bắt buộc ghi `nguon_url` là link vbpl.vn |
| Chọn văn bản không có cặp sửa đổi rõ ràng | Prototype không chứng minh được luận điểm chính | BA kiểm tra tiêu chí §1 trước khi chốt danh sách |
| Chọn toàn văn bản scan | Bị chặn bởi OCR | Cột `dinh_dang_file` trong §3 để phát hiện sớm |
| Tên cơ quan ban hành cũ do tái cơ cấu 2025 | Metadata sai, ảnh hưởng câu trả lời | Lập bảng ánh xạ tên cơ quan cũ → mới, xem §6 |

---

## 6. Bảng ánh xạ cơ quan ban hành 🔶

Do tái cơ cấu bộ máy hành chính năm 2025, tên cơ quan trong văn bản cũ khác tên hiện hành. Hệ thống cần hiển thị đúng, và người dùng cần biết văn bản do cơ quan nào ban hành theo tên tại thời điểm ban hành.

| Tên tại thời điểm ban hành | Tên hiện hành | Hiệu lực từ | Xác minh |
|---|---|---|---|
| *(điền)* | *(điền)* | *(điền)* | ⬜ |

**Chuyên gia điền bảng này.** BA không tự suy luận tên cơ quan sau tái cơ cấu — đây là thông tin phải tra từ văn bản tổ chức bộ máy.

---

## 7. Checklist hoàn thành

- [ ] 15 dòng ở §2 có số hiệu chính xác, xác minh trên `vbpl.vn`
- [ ] Mỗi dòng có đủ metadata bắt buộc ở §3
- [ ] Có **ít nhất 2 cặp sửa đổi một phần** đã liệt kê chi tiết Điều/Khoản ở §4
- [ ] Có ít nhất 1 công văn hướng dẫn để test guardrail ④
- [ ] Không quá 3 văn bản ở dạng PDF scan
- [ ] Bảng ánh xạ cơ quan ban hành (§6) đã điền
- [ ] File gốc đã tải về, tính `sha256`, lưu vào S3
- [ ] Chuyên gia XNK ký xác nhận

**Chỉ khi checklist đủ, Sprint 1 mới có căn cứ bắt đầu.**

---

## 8. Phê duyệt

| Vai trò | Người | Ngày | Chữ ký |
|---|---|---|---|
| BA (soạn cấu trúc) | | | |
| Chuyên gia XNK #1 (xác minh nội dung) | | | |
| Chuyên gia XNK #2 (đối chứng) | | | |
| Data Engineer (xác nhận dùng được) | | | |
