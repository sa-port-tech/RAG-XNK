# Ma trận Incoterms 2020 — nội dung tự biên soạn

> **Trạng thái: 🔶 Bản nháp BA — cần chuyên gia XNK rà soát, đặc biệt §5 (thực tiễn Việt Nam) và §6 (liên hệ trị giá hải quan).**
> Sở hữu: BA + Chuyên gia XNK · Hạn: Sprint 1

---

## ⚠️ Tuyên bố bản quyền

**Toàn bộ nội dung tài liệu này do đội ngũ dự án tự biên soạn.** Không sao chép câu chữ từ ấn phẩm Incoterms® 2020 của ICC.

Incoterms® là nhãn hiệu đã đăng ký của International Chamber of Commerce. Văn bản quy tắc chính thức là ấn phẩm thương mại có bản quyền — xem [`05`](05-ra-soat-ban-quyen.md) §2.1.

**Nghĩa vụ đối với hệ thống:** mọi câu trả lời sử dụng nội dung này phải kèm ghi chú:

> *Nội dung diễn giải do đội ngũ biên soạn, không phải văn bản chính thức của ICC. Để tra cứu quy tắc chính thức, tham khảo ấn phẩm Incoterms® 2020.*

---

## 1. Phân nhóm 11 điều kiện

| Nhóm | Điều kiện | Ghi chú |
|---|---|---|
| **Dùng cho mọi phương thức vận tải** | EXW · FCA · CPT · CIP · DAP · DPU · DDP | Kể cả vận tải đa phương thức |
| **Chỉ dùng cho vận tải biển và đường thủy nội địa** | FAS · FOB · CFR · CIF | Không dùng cho container giao tại bãi/CY — xem §5 |

---

## 2. Ma trận trách nhiệm

| | **Điểm chuyển rủi ro** | **Chi phí vận chuyển chính** | **Bảo hiểm** | **Thông quan XK** | **Thông quan NK** | **Dỡ hàng tại đích** |
|---|---|---|---|---|---|---|
| **EXW** | Tại cơ sở người bán, hàng đặt dưới quyền định đoạt, **chưa xếp lên phương tiện** | Người mua | Không bắt buộc | **Người mua** ⚠️ | Người mua | Người mua |
| **FCA** | Khi giao cho người vận tải do người mua chỉ định | Người mua | Không bắt buộc | Người bán | Người mua | Người mua |
| **CPT** | Khi giao cho **người vận tải đầu tiên** | **Người bán trả** tới đích | Không bắt buộc | Người bán | Người mua | Người mua |
| **CIP** | Khi giao cho **người vận tải đầu tiên** | **Người bán trả** tới đích | **Người bán — mức cao (ICC A)** | Người bán | Người mua | Người mua |
| **DAP** | Tại nơi đến, **trên phương tiện, chưa dỡ** | Người bán | Không bắt buộc | Người bán | Người mua | **Người mua** |
| **DPU** | Tại nơi đến, **sau khi đã dỡ** | Người bán | Không bắt buộc | Người bán | Người mua | **Người bán** |
| **DDP** | Tại nơi đến, trên phương tiện, chưa dỡ | Người bán | Không bắt buộc | Người bán | **Người bán** ⚠️ | Người mua |
| **FAS** | Khi đặt dọc mạn tàu tại cảng đi | Người mua | Không bắt buộc | Người bán | Người mua | Người mua |
| **FOB** | Khi hàng **đã lên tàu** tại cảng đi | Người mua | Không bắt buộc | Người bán | Người mua | Người mua |
| **CFR** | Khi hàng **đã lên tàu** tại cảng đi | **Người bán trả** tới cảng đến | Không bắt buộc | Người bán | Người mua | Người mua |
| **CIF** | Khi hàng **đã lên tàu** tại cảng đi | **Người bán trả** tới cảng đến | **Người bán — mức tối thiểu (ICC C)** | Người bán | Người mua | Người mua |

---

## 3. Điểm dễ nhầm nhất: rủi ro chuyển ≠ chi phí chuyển

Với bốn điều kiện **CPT, CIP, CFR, CIF**, hai điểm này **không trùng nhau**:

```
CIF:   [Cảng đi] ──── rủi ro chuyển tại đây (khi hàng lên tàu)
                          │
                          │  ← người bán vẫn TRẢ cước tới cảng đến
                          ▼
       [Cảng đến] ──── chi phí của người bán kết thúc tại đây
```

**Hệ quả thực tế:** hàng hư hỏng giữa đường trong hợp đồng CIF là rủi ro của **người mua**, dù người bán đã trả cước tới cảng đến. Đây là điểm người mới vào nghề hiểu sai nhiều nhất.

Với **FOB, FAS, FCA, EXW**, rủi ro và chi phí chuyển cùng một điểm — đơn giản hơn.

---

## 4. Khác biệt Incoterms 2020 so với 2010

| # | Thay đổi | Ý nghĩa thực tế |
|---|---|---|
| 1 | **DAT đổi thành DPU** | Không chỉ giao tại "terminal" nữa mà tại **bất kỳ nơi nào**, miễn là đã dỡ hàng. DPU là điều kiện duy nhất buộc người bán dỡ hàng |
| 2 | **CIP nâng mức bảo hiểm lên ICC (A)** | Bảo hiểm mọi rủi ro. **CIF vẫn giữ ICC (C)** — mức tối thiểu. Đây là điểm khác biệt lớn giữa hai điều kiện vốn hay bị coi là tương đương |
| 3 | **FCA có cơ chế vận đơn "đã xếp hàng lên tàu"** | Giải quyết vướng mắc khi thanh toán L/C yêu cầu vận đơn on-board mà điều kiện FCA vốn không tạo ra loại vận đơn này |
| 4 | **Cho phép dùng phương tiện vận tải của chính mình** | Ở FCA, DAP, DPU, DDP — trước đây ngầm giả định luôn thuê người vận tải bên ngoài |
| 5 | **Nghĩa vụ liên quan an ninh được nêu rõ hơn** | Phản ánh yêu cầu kiểm soát an ninh chuỗi cung ứng ngày càng chặt |
| 6 | **Danh mục chi phí gom về một mục** | Dễ tra "ai trả khoản gì" hơn |

**Thay đổi số 2 là thay đổi hay bị bỏ sót nhất.** Một hợp đồng CIP ký theo Incoterms 2010 và một hợp đồng CIP ký theo Incoterms 2020 có mức bảo hiểm khác nhau đáng kể.

---

## 5. Lưu ý thực tiễn Việt Nam 🔶

**Toàn bộ mục này cần chuyên gia XNK xác nhận trước khi đưa vào hệ thống.** Đây là phần giá trị nhất với người dùng và cũng là phần BA không đủ thẩm quyền tự khẳng định.

### 5.1 ⚠️ EXW với hàng xuất từ Việt Nam

Ở EXW, **nghĩa vụ thông quan xuất khẩu thuộc người mua**. Người mua nước ngoài thường không có tư cách pháp lý để đứng tên làm thủ tục hải quan xuất khẩu tại Việt Nam.

🔶 *Chuyên gia xác nhận: thực tế xử lý thế nào? Có phải các bên thường thỏa thuận người bán vẫn làm thủ tục XK dù ký EXW không? Rủi ro pháp lý của cách làm đó?*

### 5.2 ⚠️ DDP với hàng nhập vào Việt Nam

Ở DDP, **người bán chịu thông quan nhập khẩu và nộp thuế**. Người bán nước ngoài thường không có tư cách đứng tên người nhập khẩu tại Việt Nam.

🔶 *Chuyên gia xác nhận: cách xử lý trong thực tế và rủi ro tương ứng.*

### 5.3 FOB và CIF trong tập quán Việt Nam

Hai điều kiện này chiếm tỷ trọng lớn trong hợp đồng ngoại thương của doanh nghiệp Việt Nam.

🔶 *Chuyên gia xác nhận tỷ trọng thực tế và lý do (tập quán, yêu cầu ngân hàng, thói quen).*

### 5.4 ⚠️ Bốn điều kiện đường biển dùng sai cho hàng container

FAS, FOB, CFR, CIF được thiết kế cho hàng giao **tại mạn tàu hoặc trên tàu**. Với hàng container giao tại bãi (CY) hoặc kho hàng lẻ (CFS), người bán mất quyền kiểm soát hàng **trước khi** hàng lên tàu — trong khi rủi ro theo quy tắc lại chỉ chuyển khi hàng đã lên tàu.

Khoảng trống này là nguồn tranh chấp. Điều kiện phù hợp hơn cho container là **FCA, CPT, CIP**.

🔶 *Chuyên gia xác nhận mức độ phổ biến của việc dùng sai và khuyến nghị thực tế.*

---

## 6. ⚠️ Liên hệ với trị giá hải quan 🔶

**Đây là mục quan trọng nhất về mặt nghiệp vụ**, vì nó nối Incoterms (nhóm H) với trị giá hải quan (nhóm D) trong [`03`](03-pham-vi-nghiep-vu-v1.md).

Điều kiện Incoterms quyết định những khoản chi phí nào **đã nằm trong giá hợp đồng** và những khoản nào **phải cộng thêm** khi xác định trị giá hải quan hàng nhập khẩu.

| Điều kiện | Giá hợp đồng đã bao gồm | Cần xem xét cộng thêm khi tính trị giá HQ |
|---|---|---|
| EXW | Chỉ giá hàng tại xưởng | Toàn bộ chi phí đưa hàng tới cửa khẩu nhập đầu tiên |
| FOB / FAS / FCA | Giá hàng + chi phí tới điểm giao ở nước xuất | Cước vận tải quốc tế, bảo hiểm |
| CFR / CPT | Giá hàng + cước tới đích | Bảo hiểm |
| CIF / CIP | Giá hàng + cước + bảo hiểm | *(về nguyên tắc đã đủ)* |
| DAP / DPU / DDP | Đã bao gồm tới nơi đến | Có thể phải **trừ** các khoản phát sinh sau cửa khẩu nhập đầu tiên |

🔶 **Chuyên gia phải xác nhận:**
1. Nguyên tắc xác định trị giá hải quan hàng nhập khẩu quy về mốc nào theo quy định hiện hành
2. Các khoản điều chỉnh cộng và điều chỉnh trừ cụ thể
3. Cách xử lý khi hợp đồng ký DDP nhưng phải khai trị giá tại cửa khẩu nhập

**BA không viết nội dung mục này.** Bảng trên chỉ nêu *cấu trúc* của vấn đề để chuyên gia điền nội dung chính xác theo văn bản.

---

## 7. Câu hỏi mẫu để xây golden set

| Câu hỏi | Nhóm | Độ khó |
|---|---|---|
| *"CIF và CIP khác nhau ở điểm nào?"* | H | Trung bình — phải nêu cả mức bảo hiểm và phương thức vận tải |
| *"Ký FOB thì ai trả cước tàu?"* | H | Dễ |
| *"Hàng hư giữa đường trong hợp đồng CIF thì ai chịu?"* | H | Trung bình — bẫy rủi ro ≠ chi phí |
| *"DAP với DPU khác nhau chỗ nào?"* | H | Trung bình — chỉ khác ở nghĩa vụ dỡ hàng |
| *"DAT còn dùng được không?"* | H | Dễ — đã đổi thành DPU |
| *"Xuất hàng container thì nên dùng FOB hay FCA?"* | H | Khó — cần lập luận §5.4 |
| *"Nhập theo giá FOB thì trị giá hải quan tính thế nào?"* | D + H | **Khó — nối hai nhóm** |
| *"Công ty nước ngoài bán DDP vào Việt Nam có được không?"* | H | **Khó — §5.2** |

---

## 8. Cấu trúc dữ liệu

Bảng `incoterms_2020` trong structured lookup — đặc tả tại [`13`](13-dac-ta-bang-tra-cuu.md) §5.

```
incoterms_2020:
  ma            CHAR(3)      -- EXW, FCA, ...
  ten_tieng_anh TEXT
  ten_tieng_viet TEXT
  nhom          ENUM         -- moi_phuong_thuc | duong_bien
  diem_chuyen_rui_ro     TEXT
  chi_phi_van_chuyen     ENUM -- nguoi_ban | nguoi_mua
  bao_hiem               ENUM -- khong_bat_buoc | nguoi_ban_muc_toi_thieu | nguoi_ban_muc_cao
  thong_quan_xk          ENUM -- nguoi_ban | nguoi_mua
  thong_quan_nk          ENUM -- nguoi_ban | nguoi_mua
  do_hang_tai_dich       ENUM -- nguoi_ban | nguoi_mua
  luu_y_viet_nam         TEXT  -- 🔶 chuyên gia điền
  anh_huong_tri_gia_hq   TEXT  -- 🔶 chuyên gia điền
```

Hệ thống truy cập qua tool `tra_cuu_incoterms(dieu_kien)`, **không** qua vector retrieval — nội dung này có cấu trúc rõ ràng, không nên để LLM diễn giải lại từ text chunk.

---

## 9. Phê duyệt

| Vai trò | Người | Ngày | Chữ ký |
|---|---|---|---|
| BA (soạn §1–4, 7, 8) | | | |
| **Chuyên gia XNK (nội dung §5, §6)** | | | |
| Pháp chế (xác nhận tuyên bố bản quyền) | | | |
| Product Owner | | | |
