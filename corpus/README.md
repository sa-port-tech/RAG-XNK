# Corpus — vùng thu thập văn bản XNK

> Sở hữu: BA (registry) + Data Engineer (tải & hash) + Chuyên gia XNK (xác minh)
> Căn cứ: [`docs/04`](../docs/04-danh-muc-van-ban-loi.md) · [`docs/00`](../docs/00-ke-hoach-tong-the.md) §9.1–9.4 · story `E2-03`

---

## 1. Nguyên tắc: nội dung corpus KHÔNG nằm trong git

[`docs/00`](../docs/00-ke-hoach-tong-the.md) §8.1 và §18 nói rõ: git chỉ chứa code, prompt,
BPMN và golden set. Văn bản pháp luật đi qua quy trình P1 và sống trong RDS/S3.

Vì vậy thư mục này tách làm hai phần có chế độ khác nhau:

| Thư mục | Trong git? | Nội dung |
|---|---|---|
| `registry/` | ✅ **có** | Chỉ **metadata** — số hiệu, ngày, nguồn, hash. Đây là bản máy đọc được của [`docs/04`](../docs/04-danh-muc-van-ban-loi.md) §3–4–6 |
| `raw/` | ❌ **không** (`.gitignore`) | File gốc bất biến đã tải về |
| `derived/` | ❌ **không** (`.gitignore`) | Text đã trích xuất, kết quả phân rã |

Metadata nằm trong git là có chủ đích: nó là **thước đo và bằng chứng truy vết**, thay đổi
phải qua PR. Nội dung văn bản thì không — nó đi qua P1.

---

## 2. Quy ước đặt tên

### 2.1 `ma_van_ban` — định danh chuẩn

Số hiệu văn bản Việt Nam chứa dấu `/` và chữ `Đ` (`NĐ-CP`, `QĐ-TTg`) — cả hai đều gây
phiền khi dùng làm tên file, S3 key hay tham số URL. Quy tắc chuyển:

```
so_hieu  →  ma_van_ban :  1. Đ→D, đ→d
                          2. bỏ dấu thanh và dấu phụ (NFD, loại ký tự combining)
                          3. thay "/" bằng "-"
                          4. viết HOA
```

| `so_hieu` | `ma_van_ban` |
|---|---|
| `38/2015/TT-BTC` | `38-2015-TT-BTC` |
| `08/2015/NĐ-CP` | `08-2015-ND-CP` |
| `54/2014/QH13` | `54-2014-QH13` |
| `25/VBHN-BTC` | `25-VBHN-BTC` |

**Đây là cặp định danh / giá trị hiển thị, đừng lẫn hai vai:**

| Trường | Vai | Dấu tiếng Việt |
|---|---|---|
| `so_hieu` | Sự thật, dùng để hiển thị và trích dẫn | ✅ giữ nguyên |
| `ma_van_ban` | Định danh: khoá registry, tên file, S3 key, log | ❌ đã gấp về ASCII |

Trích dẫn hiển thị cho người dùng **luôn dùng `so_hieu`**. Không bao giờ hiển thị
`ma_van_ban` — `08-2015-ND-CP` không phải cách một khai báo viên viết số hiệu văn bản.

`ma_van_ban` là khoá chính của registry, và là chuỗi mà `E3-09` (chuẩn hoá số hiệu về dạng
canonical) phải sinh ra được từ mọi biến thể người dùng gõ.

### 2.2 Tên file

```
{ma_van_ban}__{ngay_ban_hanh}.{duoi}
```

Ví dụ: `38-2015-TT-BTC__2015-03-25.pdf`

Dùng **hai** dấu gạch dưới làm ranh giới vì bản thân `ma_van_ban` đã chứa dấu `-`. Ngày ở
dạng `YYYY-MM-DD` để sắp xếp theo thứ tự thời gian bằng sort chuỗi thông thường.

Phụ lục tách rời đặt thêm hậu tố: `39-2018-TT-BTC__2018-04-20__phu-luc-1.pdf`

---

## 3. Phân loại

Mỗi văn bản mang **ba** trục phân loại độc lập — đừng gộp chúng:

| Trục | Giá trị | Dùng để |
|---|---|---|
| `nhom_corpus` | `A` `B` `C` `D` | Vị trí trong [`docs/04`](../docs/04-danh-muc-van-ban-loi.md) §2 — kiểm tra độ phủ của bộ 15 văn bản |
| `nhom_nghiep_vu` | `A`…`K` (nhiều giá trị) | Nhóm nghiệp vụ [`docs/03`](../docs/03-pham-vi-nghiep-vu-v1.md) §3.1 — nối tới golden set và ma trận truy vết |
| `vai_tro` | xem dưới | Vai trò trong việc chứng minh luận điểm kiến trúc |

`vai_tro` nhận một trong:

| Giá trị | Nghĩa |
|---|---|
| `nen_tang` | Luật/Nghị định làm nền khái niệm |
| `bi_sua_doi` | Văn bản **bị** một văn bản khác sửa đổi một phần — vế trái của cặp kiểm chứng |
| `van_ban_sua_doi` | Văn bản **đi sửa** văn bản khác — vế phải của cặp kiểm chứng |
| `vbhn` | Văn bản hợp nhất — dùng kiểm chứng BR-09 |
| `cong_van` | Công văn hướng dẫn — dùng kiểm chứng guardrail ④ |
| `phu_luc` | Phụ lục/biểu mẫu tách rời |

---

## 4. Vòng đời một bản ghi

```
de_xuat  →  da_xac_minh  →  da_tai  →  da_doi_chieu
```

| Trạng thái | Điều kiện chuyển sang |
|---|---|
| `de_xuat` | BA đưa vào danh sách ứng viên |
| `da_xac_minh` | **Chuyên gia XNK** xác nhận số hiệu + ngày + trạng thái hiệu lực trên `vbpl.vn`, điền `xac_minh_vbpl` |
| `da_tai` | File gốc đã tải, có `sha256`, đã đẩy S3 |
| `da_doi_chieu` | Chuyên gia đã đối chiếu nội dung file với bản gốc trên nguồn |

**Chỉ bản ghi ở `da_xac_minh` trở lên mới được `tools/corpus/thu_thap.py` tải.** Script từ
chối tải bản ghi còn ở `de_xuat` — có chủ đích, để không ai lỡ tay ingest một số hiệu chưa
ai kiểm.

---

## 5. Cảnh báo về nguồn metadata

Registry ghi rõ **từng trường lấy từ đâu** trong khối `metadata_nguon`. Lý do:

[`docs/04`](../docs/04-danh-muc-van-ban-loi.md) §5 xếp *"lấy số hiệu từ Google thay vì
`vbpl.vn`"* là một rủi ro có tên. Nhưng rủi ro thật hẹp hơn và cụ thể hơn thế:

> **Công báo Chính phủ cho biết văn bản "có hiệu lực" hay không ở **cấp văn bản**. Nó
> không cho biết Điều nào bên trong đã bị thay thế.**

Đó chính xác là khoảng trống mà cả hệ thống này sinh ra để lấp ([`docs/00`](../docs/00-ke-hoach-tong-the.md)
§9.4). Vì vậy trường `trang_thai` trong registry **để trống cho tới khi chuyên gia điền từ
`vbpl.vn`**, kể cả khi Công báo đã hiển thị một trạng thái.

---

## 6. Lệnh

```bash
python tools/corpus/kiem_tra.py
```

```bash
python tools/corpus/thu_thap.py --dry-run
```

Chi tiết: [`tools/corpus/README.md`](../tools/corpus/README.md)
