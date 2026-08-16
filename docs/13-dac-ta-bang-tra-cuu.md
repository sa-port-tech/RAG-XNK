# Đặc tả bảng tra cứu có cấu trúc

> **Trạng thái: 🔶 Bản nháp BA — nội dung dữ liệu do chuyên gia XNK cung cấp và đối chiếu 100%.**
> Sở hữu: BA (đặc tả) + Chuyên gia XNK (dữ liệu) + Data Engineer (triển khai) · Hạn: Sprint 1–2

---

## 1. Nguyên tắc: bảng không đi qua RAG

Năm nhóm dữ liệu trong tài liệu này **phải truy vấn từ bảng quan hệ qua tool calling**, không được để LLM sinh ra từ text chunk.

**Lý do:** biểu thuế và danh mục HS là bảng nhiều nghìn dòng. Nếu để LLM đọc từ text chunk rồi trả lời, nó sẽ bịa thuế suất — và bịa một cách rất thuyết phục. Đưa qua tool calling loại bỏ hoàn toàn khả năng này, vì con số đến từ database chứ không từ model.

```
Câu hỏi về thuế suất
   → LLM gọi tool tra_cuu_thue_suat(ma_hs, nuoc_xuat_xu, ngay)
   → Tool truy vấn bảng bieu_thue
   → Trả về dòng dữ liệu + điều kiện áp dụng
   → LLM diễn giải KẾT QUẢ, không tự sinh con số
```

---

## 2. `danh_muc_hs`

| Trường | Kiểu | Ghi chú |
|---|---|---|
| `ma_hs` | CHAR(8) | Khóa chính. Lưu cả dạng có dấu chấm và không dấu chấm để tra cứu |
| `ma_chuong` | CHAR(2) | 2 số đầu |
| `ma_nhom` | CHAR(4) | 4 số đầu |
| `ma_phan_nhom` | CHAR(6) | 6 số đầu |
| `mo_ta` | TEXT | Mô tả hàng hóa |
| `mo_ta_cha` | TEXT | Mô tả của cấp trên, để hiển thị ngữ cảnh |
| `don_vi_tinh` | VARCHAR | |
| `ghi_chu_phan_loai` | TEXT | Chú giải được nội luật hoá |
| `nguon_van_ban_id` | FK | Thông tư Danh mục hàng hóa XNK VN |
| `ngay_hieu_luc` / `ngay_het_hieu_luc` | DATE | **Có phiên bản** — danh mục thay đổi theo kỳ AHTN |

**Nguồn:** Thông tư của Bộ Tài chính ban hành Danh mục hàng hóa XNK Việt Nam. **Không lấy từ tài liệu WCO** (có bản quyền — [`05`](05-ra-soat-ban-quyen.md) §2.3).

### ⚠️ Ràng buộc guardrail ①

Tool `tra_cuu_ma_hs` **trả về ứng viên, không trả về kết luận**. Cấu trúc phản hồi bắt buộc:

```json
{
  "ung_vien": [ { "ma_hs": "...", "mo_ta": "...", "do_khop": 0.0 } ],
  "canh_bao": "Kết quả tra cứu tham khảo. Việc phân loại hàng hóa và áp mã HS
               chính thức thuộc thẩm quyền của cơ quan hải quan.",
  "huong_dan": "Để có mã số chính thức, doanh nghiệp có thể làm thủ tục
                xác định trước mã số hàng hóa."
}
```

Trường `canh_bao` và `huong_dan` **luôn có mặt**, không phụ thuộc vào việc LLM có nhắc tới hay không. Post-processing kiểm tra câu trả lời cuối có chứa cảnh báo — nếu thiếu thì chặn (E4-03 ⚙️).

---

## 3. `bieu_thue`

**Đây là bảng rủi ro cao nhất trong tài liệu này.** Trả sai thuế suất gây thiệt hại tài chính trực tiếp.

| Trường | Kiểu | Ghi chú |
|---|---|---|
| `id` | PK | |
| `ma_hs` | CHAR(8) | FK |
| `loai_thue` | ENUM | `nk_uu_dai` · `nk_thong_thuong` · `nk_uu_dai_dac_biet` · `xuat_khau` · `vat` · `ttdb` · `bvmt` · `tu_ve` · `chong_ban_pha_gia` |
| `hiep_dinh` | VARCHAR | Chỉ với `nk_uu_dai_dac_biet` — ATIGA, ACFTA, VKFTA… |
| `thue_suat` | DECIMAL | |
| `don_vi` | ENUM | `phan_tram` · `tuyet_doi` |
| `dieu_kien_ap_dung` | TEXT | **Bắt buộc, không được rỗng** — xem dưới |
| `nguon_van_ban_id` | FK | Nghị định biểu thuế |
| `ngay_hieu_luc` / `ngay_het_hieu_luc` | DATE | **Bắt buộc — bảng có phiên bản** |

### ⚠️ Ràng buộc guardrail ② — `dieu_kien_ap_dung` không được rỗng

Với `nk_uu_dai_dac_biet`, thuế suất chỉ áp dụng khi đáp ứng điều kiện — thường gồm C/O hợp lệ theo mẫu tương ứng và đáp ứng quy tắc xuất xứ của hiệp định.

**Trả thuế suất ưu đãi mà không nêu điều kiện là câu trả lời sai về mặt nghiệp vụ**, kể cả khi con số đúng. Người dùng sẽ tưởng cứ nhập từ nước đó là được hưởng.

Ràng buộc kỹ thuật:
- `CHECK (loai_thue <> 'nk_uu_dai_dac_biet' OR dieu_kien_ap_dung IS NOT NULL)`
- Tool `tra_cuu_thue_suat` luôn trả `dieu_kien_ap_dung` kèm `thue_suat`
- Post-processing kiểm tra câu trả lời có nêu điều kiện (E4-04 ⚙️)

### ⚠️ Quy trình nạp dữ liệu

Bảng này **không đi qua pipeline text chung**. Quy trình bắt buộc:

1. Trích riêng thành CSV/Excel có cấu trúc
2. **Chuyên gia đối chiếu 100% với bản gốc** — không lấy mẫu
3. Import vào database
4. Chạy kiểm tra toàn vẹn: mọi `ma_hs` tồn tại trong `danh_muc_hs`; mọi dòng có `ngay_hieu_luc`
5. Chuyên gia ký xác nhận trước khi bật cho người dùng

🔶 *Chuyên gia xác nhận: có bao nhiêu loại thuế cần đưa vào v1? Danh sách `loai_thue` ở trên đã đủ chưa?*

---

## 4. `ma_loai_hinh`

| Trường | Kiểu | Ghi chú |
|---|---|---|
| `ma` | VARCHAR(4) | Mã loại hình tờ khai |
| `chieu` | ENUM | `xuat_khau` · `nhap_khau` |
| `ten` | TEXT | |
| `mo_ta_chi_tiet` | TEXT | |
| `dieu_kien_ap_dung` | TEXT | **Quan trọng nhất** — khi nào được dùng mã này |
| `truong_hop_thuong_gap` | TEXT | Ví dụ tình huống thực tế |
| `ma_lien_quan` | VARCHAR[] | Các mã dễ nhầm với mã này |
| `nguon_van_ban_id` | FK | |
| `ngay_hieu_luc` / `ngay_het_hieu_luc` | DATE | |

🔶 **Toàn bộ nội dung bảng này do chuyên gia cung cấp từ văn bản hiện hành.** BA không liệt kê mã loại hình vì đây là dữ liệu thay đổi theo Thông tư và sai một mã là hỏng cả tờ khai.

**Trường `ma_lien_quan` là giá trị riêng của hệ thống:** khi người dùng hỏi về một mã, hệ thống chủ động nêu các mã dễ nhầm — đây chính là lỗi phổ biến nhất của khai báo viên mới.

---

## 5. `incoterms_2020`

Nội dung tại [`12`](12-ma-tran-incoterms-2020.md). Schema tại [`12`](12-ma-tran-incoterms-2020.md) §8.

**Nguồn: tự biên soạn.** Không ingest tài liệu ICC.

---

## 6. `nghiep_vu_vnaccs` ⚠️

| Trường | Kiểu |
|---|---|
| `ma` | VARCHAR(8) |
| `ten_nghiep_vu` | TEXT |
| `mo_ta` | TEXT |
| `chieu` | ENUM |
| `do_on_dinh` | ENUM — `cao` · `thap` |

**⚠️ Gắn cờ `do_on_dinh = 'thap'` cho toàn bộ bảng này.** VNACCS/VCIS đang trong lộ trình thay thế bởi hệ thống CNTT hải quan mới. Bảng cập nhật độc lập với phần còn lại của corpus, và câu trả lời dùng dữ liệu này nên kèm ghi chú về khả năng thay đổi.

Nội dung: [`11`](11-tu-dien-thuat-ngu-xnk.md) §8 — cần chuyên gia xác minh.

---

## 7. Đặc tả tool

| Tool | Tham số | Trả về | Guardrail |
|---|---|---|---|
| `tra_cuu_ma_hs` | `mo_ta_hang_hoa` \| `ma_hs` | Ứng viên + **cảnh báo** + hướng dẫn xác định trước | ① ⚙️ |
| `tra_cuu_thue_suat` | `ma_hs`, `nuoc_xuat_xu?`, `ngay?` | Các dòng thuế + **`dieu_kien_ap_dung`** | ② ⚙️ |
| `tra_cuu_loai_hinh` | `ma` \| `mo_ta_giao_dich` | Mã + điều kiện + **mã dễ nhầm** | |
| `tra_cuu_incoterms` | `dieu_kien` | Ma trận trách nhiệm + ghi chú bản quyền | |
| `tra_cuu_van_ban` | `so_hieu` \| `tu_khoa`, `ngay_tham_chieu?` | Nội dung + trạng thái hiệu lực | |
| `kiem_tra_hieu_luc` | `so_hieu`, `dieu?`, `khoan?`, `ngay?` | Còn/hết hiệu lực + VB thay thế | |

**Tham số `ngay` có mặt ở mọi tool truy vấn dữ liệu có phiên bản.** Mặc định là hôm nay; khi câu hỏi chứa mốc thời gian, `chat-orchestrator` truyền mốc đó xuống (BR-03).

Dùng `strict: true` trên tool có schema chặt.

---

## 8. Cổng chất lượng dữ liệu

| Bảng | Điều kiện bật cho người dùng |
|---|---|
| `danh_muc_hs` | Chuyên gia đối chiếu ≥ 10% ngẫu nhiên, sai số 0 |
| **`bieu_thue`** | **Chuyên gia đối chiếu 100%**, ký xác nhận |
| `ma_loai_hinh` | Chuyên gia đối chiếu 100% |
| `incoterms_2020` | Chuyên gia rà toàn bộ, đặc biệt §5–6 của [`12`](12-ma-tran-incoterms-2020.md) |
| `nghiep_vu_vnaccs` | Chuyên gia xác minh, gắn cờ độ ổn định thấp |

Mọi bảng đều phải qua kiểm tra tự động: không dòng nào thiếu `ngay_hieu_luc`; khóa ngoại toàn vẹn; không có khoảng trống hoặc chồng lấn phiên bản trên cùng một khóa.

---

## 9. Phạm vi prototype

| Bảng | Prototype | Ghi chú |
|---|---|---|
| `incoterms_2020` | ✅ Đầy đủ 11 điều kiện | Tự biên soạn, không phụ thuộc corpus |
| `ma_loai_hinh` | ✅ Đầy đủ | Cần cho nhóm B (P0) |
| `danh_muc_hs` | 🔶 **Chờ trả lời Q1** trong [`03`](03-pham-vi-nghiep-vu-v1.md) §7 | Nếu nhóm K vào v1 thì cần tập mẫu ~200 dòng để test guardrail ① |
| `bieu_thue` | ⏸ Hoãn | Rủi ro cao, khối lượng đối chiếu lớn. Đưa vào Phase 1 |
| `nghiep_vu_vnaccs` | ⏸ Hoãn | Ưu tiên P2 |

**Đề xuất của BA:** ngay cả khi hoãn `bieu_thue`, vẫn nên nạp **một tập mẫu nhỏ** (20–30 dòng) để kiểm chứng guardrail ② hoạt động. Không có dữ liệu thì không test được quy tắc, và quy tắc chưa được test thì coi như chưa có.

---

## 10. Phê duyệt

| Vai trò | Người | Ngày | Chữ ký |
|---|---|---|---|
| BA (đặc tả) | | | |
| Chuyên gia XNK (dữ liệu + đối chiếu) | | | |
| Data Engineer (triển khai) | | | |
| AI Engineer (tool calling) | | | |
