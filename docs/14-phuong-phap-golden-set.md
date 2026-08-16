# Phương pháp xây dựng Golden Set

> **Trạng thái: ✅ Phương pháp hoàn thiện — nội dung câu hỏi do chuyên gia XNK tạo.**
> Sở hữu: BA (phương pháp) + Chuyên gia XNK (nội dung) · Dữ liệu: `eval/golden-set/`

---

## 1. Vì sao tài liệu này quan trọng

Golden set là **tài sản giá trị nhất của dự án và không thể mua được**. Nó là thứ duy nhất cho phép trả lời câu hỏi *"hệ thống có đúng không?"* bằng số liệu thay vì bằng cảm giác.

Nó cũng là thứ dễ làm hỏng nhất: 200 câu hỏi sinh tự động bằng LLM trông giống golden set nhưng vô giá trị, vì chúng đo khả năng hệ thống trả lời những câu mà chính LLM nghĩ ra — không phải những câu người dùng thật hỏi.

**Nguyên tắc: chất lượng hơn số lượng.** 40 câu chuyên gia viết cẩn thận có giá trị hơn 200 câu sinh máy.

---

## 2. Chỉ tiêu

| Giai đoạn | Số câu | Câu bẫy hiệu lực | Câu ngoài phạm vi |
|---|---|---|---|
| Sprint 0 (vòng 1) | 20 | ≥ 5 | 5 |
| Sprint 1 (đầy đủ prototype) | 40–50 | **≥ 10** | 20 |
| Phase 1 (production) | 300–500 | ≥ 30 | 50 |

Production: tối thiểu 50 câu mỗi nhóm phân loại (§4).

---

## 3. Cấu trúc một mục

```yaml
id: GS-001
cau_hoi: "Điều 18 Thông tư 38/2015/TT-BTC còn hiệu lực không?"
cau_hoi_bien_the:                    # cách hỏi khác của cùng nội dung
  - "TT 38 điều 18 còn áp dụng ko"
  - "Điều 18 thông tư 38 bị sửa chưa"
dap_an_chuan: |
  <chuyên gia viết bằng lời của mình>
can_cu_phap_ly:
  - so_hieu: "38/2015/TT-BTC"
    dieu: 18
    khoan: null
    trang_thai: "het_hieu_luc_mot_phan"
    van_ban_thay_the: "39/2018/TT-BTC"
phan_loai: hieu_luc                  # §4
do_kho: trung_binh                   # de | trung_binh | kho | gay_tranh_cai
la_cau_bay: true
ngay_tham_chieu: null                # null = hôm nay
nguoi_tao: "<tên chuyên gia>"
ngay_tao: 2026-08-20
ghi_chu: "Ca kinh điển: văn bản còn hiệu lực nhưng Điều bên trong đã bị thay thế"
```

**Trường `can_cu_phap_ly` là bắt buộc.** Không có nó thì không đo được `Citation Precision` — chỉ số quan trọng thứ hai sau `Stale Citation Rate`.

**Trường `cau_hoi_bien_the` đo tính bền của retrieval.** Người dùng thật gõ tắt, sai chính tả, trộn tiếng Anh. Nếu hệ thống chỉ trả lời đúng bản viết chuẩn thì nó chưa dùng được.

---

## 4. Phân loại

| Mã | Nhóm | Nhóm nghiệp vụ ([`03`](03-pham-vi-nghiep-vu-v1.md)) | Tỷ trọng prototype |
|---|---|---|---|
| `thu_tuc` | Thủ tục hải quan | A | 25% |
| `loai_hinh` | Loại hình tờ khai | B | 15% |
| **`hieu_luc`** | **Hiệu lực văn bản** | **C** | **25%** |
| `tri_gia` | Trị giá hải quan | D | 5% |
| `xuat_xu` | Xuất xứ & C/O | E | 10% |
| `thue` | Thuế XNK | F | 5% |
| `incoterms` | Incoterms | H | 10% |
| `phan_loai_hs` | Tra cứu HS | K | 5% |
| `ngoai_pham_vi` | Ngoài phạm vi | — | *(20 câu riêng)* |

**Nhóm `hieu_luc` chiếm tỷ trọng cao nhất có chủ đích** — nó là luận điểm cốt lõi mà prototype tồn tại để chứng minh.

---

## 5. Câu bẫy hiệu lực — cách xây

Câu bẫy là câu mà **đáp án đúng nằm ở văn bản đã bị sửa đổi**. Hệ thống không có bộ lọc hiệu lực sẽ trả lời tự tin theo nội dung cũ.

### Năm dạng bẫy

| Dạng | Mô tả | Ví dụ khung |
|---|---|---|
| **B1 — Điều bị thay thế** | Hỏi về Điều đã bị Thông tư sau sửa đổi | *"Theo TT 38, thủ tục X thế nào?"* trong khi Điều đó đã bị TT 39 thay |
| **B2 — Truy vấn quá khứ** | Hỏi quy định tại thời điểm cũ | *"Tờ khai mở tháng 3/2023 áp dụng quy định nào?"* |
| **B3 — Văn bản còn hiệu lực, Điều đã hết** | Hỏi trực tiếp về trạng thái | *"Điều X của TT Y còn hiệu lực không?"* |
| **B4 — Trích dẫn chéo** | Văn bản A dẫn chiếu Điều của văn bản B, mà Điều đó đã bị sửa | Kiểm tra hệ thống có lần theo được chuỗi dẫn chiếu |
| **B5 — Văn bản hợp nhất** | Có VBHN, hỏi nội dung hiện hành | Kiểm tra BR-09 — hệ thống ưu tiên VBHN |

### Yêu cầu tối thiểu prototype

- ≥ 3 câu dạng B1
- ≥ 3 câu dạng B2
- ≥ 2 câu dạng B3
- ≥ 1 câu dạng B4
- ≥ 1 câu dạng B5

**Toàn bộ câu bẫy phải trả lời đúng bằng truy vấn SQL thuần ở Sprint 1**, trước khi có LLM. Đây là cổng chất lượng của E2-02.

---

## 6. Câu ngoài phạm vi — đo `Correct Refusal Rate`

20 câu, lấy từ [`03`](03-pham-vi-nghiep-vu-v1.md) §3.2 và §5. Cấu trúc:

```yaml
id: OOS-001
cau_hoi: "Nhập lô hàng này thì làm sao đóng ít thuế nhất?"
loai_tu_choi: tu_van_toi_uu_thue
hanh_vi_mong_doi: |
  Từ chối tư vấn tối ưu thuế. Có thể nêu các quy định liên quan
  về miễn giảm nhưng không đưa ra khuyến nghị cho trường hợp cụ thể.
  Đề nghị làm việc với chuyên gia thuế.
```

**Bẫy khó nhất trong nhóm này:** câu hỏi *nghe có vẻ* trong phạm vi nhưng thực ra không. Ví dụ *"Mặt hàng này mã HS là gì?"* — nằm trong nhóm K nhưng đòi hỏi hành vi guardrail đặc biệt, không phải trả lời thẳng.

---

## 7. Quy trình xây — buổi workshop

**Không gửi form cho chuyên gia rồi chờ.** BA ngồi cùng, ghi chép, hỏi lại. Chuyên gia biết nghiệp vụ nhưng không quen viết đặc tả kiểm chứng được — đó là việc của BA.

### Buổi 1 — Sprint 0, 3 giờ, mục tiêu 20 câu

| Thời lượng | Nội dung |
|---|---|
| 20' | BA trình bày mục đích: đây là thước đo, không phải FAQ |
| 40' | **Chuyên gia kể 10 câu hỏi thực tế gần đây nhất họ nhận được** — không nghĩ ra câu mới |
| 60' | Với mỗi câu: chuyên gia viết đáp án + tra căn cứ pháp lý trên `vbpl.vn` |
| 40' | BA hướng dẫn xây câu bẫy theo 5 dạng ở §5 |
| 20' | Chốt 5 câu ngoài phạm vi |

**Kỹ thuật quan trọng ở phần 40 phút:** hỏi *"câu hỏi gần đây nhất bạn nhận được là gì?"* thay vì *"người dùng thường hỏi gì?"*. Câu hỏi thứ hai cho ra câu trả lời khái quát và đã được làm mượt; câu thứ nhất cho ra dữ liệu thật, kể cả khi nó lộn xộn.

### Buổi 2 — Sprint 1, 3 giờ, mục tiêu 40–50 câu

- Bổ sung theo tỷ trọng ở §4
- Hai chuyên gia **đối chứng chéo**: mỗi người rà đáp án của người kia
- **Ghi lại chỗ hai chuyên gia không đồng ý** — xem §8

### Buổi 3 — Sprint 3, 2 giờ

- Chấm điểm 30 câu ngẫu nhiên trên hệ thống thật
- Bổ sung câu từ những lỗi phát hiện được

---

## 8. ⚠️ Xử lý khi hai chuyên gia không đồng ý

**Đây là tình huống có giá trị nhất, không phải vấn đề cần dẹp.**

Nghiệp vụ XNK có nhiều điểm mà hai người có kinh nghiệm hiểu khác nhau — thường là chỗ văn bản không rõ, hoặc thực tiễn địa phương khác nhau. Chính những chỗ đó là chỗ hệ thống dễ sai nhất và người dùng cần được cảnh báo nhất.

Cách xử lý:

1. **Không ép đồng thuận.** Ghi cả hai cách hiểu vào `ghi_chu`
2. Gắn `do_kho: gay_tranh_cai`
3. **Loại khỏi tập tính điểm chính** — không công bằng khi chấm hệ thống bằng câu mà chuyên gia còn chưa thống nhất
4. Đưa vào tập riêng `eval/golden-set/gay-tranh-cai/`
5. **Hành vi mong đợi của hệ thống với nhóm này: nêu cả hai cách hiểu và khuyến nghị tham vấn cơ quan hải quan** — chứ không phải chọn một bên

Số lượng câu gây tranh cãi là **chỉ số riêng đáng theo dõi**: nó cho biết vùng nào của nghiệp vụ mơ hồ và cần thận trọng nhất.

---

## 9. Lưu trữ & quản trị

```
eval/golden-set/
├── cau-hoi/
│   ├── thu-tuc.yaml
│   ├── hieu-luc.yaml          ← quan trọng nhất
│   ├── loai-hinh.yaml
│   ├── incoterms.yaml
│   └── ...
├── ngoai-pham-vi.yaml
├── gay-tranh-cai/
├── baseline.json              ← chỉ số baseline trên main
└── README.md
```

| Quy tắc | Lý do |
|---|---|
| Versioned trong git | Thay đổi golden set là thay đổi thước đo — phải có lịch sử |
| `CODEOWNERS`: `/eval/golden-set/ @xnk-expert-team` | Chỉ chuyên gia được duyệt thay đổi |
| Thêm câu mới → chạy lại baseline | Không so sánh chỉ số qua hai phiên bản golden set khác nhau |
| Mỗi câu ghi `nguoi_tao` và `ngay_tao` | Truy vết khi có tranh cãi |

---

## 10. Chống các lỗi thường gặp

| Lỗi | Vì sao hỏng | Cách tránh |
|---|---|---|
| **Sinh câu hỏi bằng LLM** | Đo khả năng trả lời câu LLM nghĩ ra, không phải câu người dùng hỏi | Chỉ dùng câu thật từ chuyên gia và người dùng |
| **Chỉ có câu dễ** | Chỉ số đẹp nhưng vô nghĩa | Ràng buộc tỷ trọng độ khó: 30% dễ / 50% trung bình / 20% khó |
| **Đáp án viết quá ngắn** | Không đủ để LLM-as-judge chấm | Đáp án tối thiểu 3 câu, nêu rõ căn cứ |
| **Thiếu căn cứ pháp lý** | Không đo được Citation Precision | Trường bắt buộc, CI kiểm tra |
| **Không có câu bẫy** | Bỏ sót luận điểm cốt lõi | Tối thiểu 10 câu, đủ 5 dạng |
| **Sửa golden set để chỉ số đẹp hơn** | Gian lận với chính mình | CODEOWNERS + review bắt buộc + ghi lịch sử |

Lỗi cuối cùng là lỗi nguy hiểm nhất và khó phát hiện nhất. Khi chỉ số không đạt, phản xạ tự nhiên là nghi ngờ câu hỏi. **Quy tắc: chỉ sửa câu hỏi khi chuyên gia xác nhận đáp án chuẩn sai, không bao giờ vì hệ thống trả lời khác.**

---

## 11. Sử dụng trong CI

| Chỉ số | Ngưỡng | Hành vi khi không đạt |
|---|---|---|
| `Stale Citation Rate` | **= 0** | **Chặn merge tuyệt đối** |
| `Recall@10` | ≥ 0,90 | Chặn merge |
| `Citation Precision` | ≥ 0,90 | Cảnh báo |
| `Correct Refusal Rate` | ≥ 0,90 | Cảnh báo |
| Bất kỳ chỉ số nào | Tụt > 2% so với baseline | Chặn merge |

Workflow `eval-regression.yml` chạy khi PR đụng `prompts/`, `retrieval/`, `generation/`, `eval/` — chi tiết tại [`09`](09-product-backlog.md) E7-05.

---

## 12. Phê duyệt

| Vai trò | Người | Ngày | Chữ ký |
|---|---|---|---|
| BA (phương pháp) | | | |
| Chuyên gia XNK #1 (nội dung) | | | |
| Chuyên gia XNK #2 (đối chứng) | | | |
| AI Engineer (dùng được cho eval) | | | |
| Product Owner | | | |
