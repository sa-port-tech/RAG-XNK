# Ma trận truy vết yêu cầu

> **Trạng thái: ⬜ Khung — cập nhật liên tục trong suốt dự án.**
> Sở hữu: BA · Cập nhật: cuối mỗi sprint

---

## 1. Mục đích

Ma trận này trả lời bốn câu hỏi mà không có nó thì phải đoán:

| Câu hỏi | Ai hỏi |
|---|---|
| Quy tắc nghiệp vụ này đã được cài đặt chưa, ở đâu? | PO khi nghiệm thu |
| Story này có test không, test nào? | Tech Lead khi review |
| Nếu sửa chỗ này thì hỏng cái gì? | Developer khi refactor |
| Chỉ số eval này đo quy tắc nào? | Chuyên gia khi chấm điểm |

**Nguyên tắc: mỗi quy tắc nghiệp vụ phải truy được tới ít nhất một test tự động hoặc một tiêu chí chấm của chuyên gia.** Quy tắc không có đường truy vết là quy tắc không ai kiểm chứng — tức là quy tắc chưa tồn tại.

---

## 2. Chuỗi truy vết

```
Nhu cầu người dùng (phỏng vấn / phạm vi)
        ↓
Quy tắc nghiệp vụ  BR-xx   ([03] §4)
        ↓
Story  Exx-yy   ([09])
        ↓
Cài đặt  (service · file)
        ↓
Test  (unit · integration · eval)
        ↓
Chỉ số  (CI · chuyên gia chấm)
```

---

## 3. Ma trận chính — quy tắc nghiệp vụ

| BR | Quy tắc | Story | Cài đặt | Test | Chỉ số |
|---|---|---|---|---|---|
| BR-01 | Mọi khẳng định có trích dẫn + ngày hiệu lực | E4-02 | `generation` | eval | `Citation Precision` |
| **BR-02** | **Không trả về điều khoản hết hiệu lực** | **E3-05** | `retrieval` (SQL) | ⚙️ required | **`Stale Citation Rate = 0`** |
| BR-03 | Câu hỏi có mốc thời gian → dùng mốc đó | E3-08 | `retrieval` | ⚙️ | eval nhóm `hieu_luc` |
| BR-04 | Câu trả lời có mã HS phải kèm cảnh báo | E4-03 | `generation` post-proc | ⚙️ required | 100% nhóm `phan_loai_hs` |
| BR-05 | Thuế suất ưu đãi phải nêu điều kiện | E4-04 | `generation` post-proc | ⚙️ | eval nhóm `thue` |
| BR-06 | Công văn phải gắn nhãn phân biệt | E4-06 | `generation` post-proc | ⚙️ | eval |
| **BR-07** | **Không đủ căn cứ thì từ chối** | **E4-05** | `generation` | ⚙️ | **`Correct Refusal Rate ≥ 0,90`** |
| BR-08 | Ba khái niệm thông quan/giải phóng/bảo quản không gộp | E4-14, E3-10 | `generation` + từ điển | eval | Chuyên gia chấm |
| BR-09 | Ưu tiên trích dẫn VBHN | E2-10 | `corpus` | unit | eval câu bẫy B5 |
| BR-10 | Disclaimer cuối mỗi câu trả lời | E4-08 | `generation` | ⚙️ | 100% câu trả lời |
| **BR-11** | **Cách ly dữ liệu giữa tenant** | **E5-04** | `identity-tenant` + SQL | ⚙️ required | **0 rò rỉ / 50 truy vấn chéo** |

**Ba dòng in đậm là ba required status check** — không pass thì không merge được.

---

## 4. Ma trận guardrail

| Guardrail | BR | Story | Thực thi ở đâu | Kiểm chứng |
|---|---|---|---|---|
| ① Mã HS | BR-04 | E4-03 | **Code post-processing**, không chỉ prompt | ⚙️ 100% nhóm K |
| ② Thuế suất | BR-05 | E4-04 | Code post-processing + ràng buộc DB `CHECK` | ⚙️ |
| ③ Văn bản hết hiệu lực | BR-02 | E4-07 | Code post-processing + bộ lọc SQL | ⚙️ |
| ④ Công văn | BR-06 | E4-06 | Metadata + code | ⚙️ |
| ⑤ Từ chối | BR-07 | E4-05 | Ngưỡng rerank + prompt + judge | ⚙️ |

> **Cột "thực thi ở đâu" là cột quan trọng nhất.** Guardrail chỉ nằm trong system prompt là guardrail có thể bị model bỏ qua. Cả năm đều phải có lớp kiểm tra bằng code ở tầng post-processing.

---

## 5. Ma trận nhóm nghiệp vụ → dữ liệu → story

| Nhóm ([`03`](03-pham-vi-nghiep-vu-v1.md) §3.1) | Nguồn dữ liệu | Story chính | Trong prototype? |
|---|---|---|---|
| A — Thủ tục hải quan | 15 văn bản lõi | E2-05, E3-04 | ✅ |
| B — Loại hình tờ khai | `ma_loai_hinh` | E4-11 | ✅ |
| **C — Hiệu lực văn bản** | **Đồ thị hiệu lực** | **E2-02, E3-05, E3-08** | ✅ **trọng tâm** |
| D — Trị giá hải quan | Văn bản lõi | E3-04 | ✅ hạn chế |
| E — Xuất xứ & C/O | Văn bản lõi + FTA | E3-04 | ✅ hạn chế |
| F — Thuế XNK | `bieu_thue` | E4-04, E4-11 | 🔶 tập mẫu |
| G — Kiểm tra chuyên ngành | Chưa có văn bản trong corpus | — | ❌ Phase 1 |
| H — Incoterms | `incoterms_2020` ([`12`](12-ma-tran-incoterms-2020.md)) | E4-11 | ✅ |
| I — Thanh toán quốc tế | — | — | ❌ v2 (bản quyền UCP) |
| J — VNACCS | `nghiep_vu_vnaccs` | — | ❌ Phase 1 |
| K — Tra cứu HS | `danh_muc_hs` | E4-03 | 🔶 chờ Q1 |

---

## 6. Ma trận tài liệu → story

| Tài liệu | Cung cấp đầu vào cho |
|---|---|
| [`03`](03-pham-vi-nghiep-vu-v1.md) Phạm vi | Toàn bộ backlog · tập câu ngoài phạm vi |
| [`04`](04-danh-muc-van-ban-loi.md) Danh mục văn bản | E2-03, E2-07, **E2-08** |
| [`05`](05-ra-soat-ban-quyen.md) Bản quyền | E2-03 (được ingest gì) · E4-08 (disclaimer) |
| [`10`](10-dac-ta-quy-trinh-bpmn.md) BPMN | E6-01 … E6-07 |
| [`11`](11-tu-dien-thuat-ngu-xnk.md) Từ điển | E3-10, E4-12 |
| [`12`](12-ma-tran-incoterms-2020.md) Incoterms | E4-11 · nhóm H golden set |
| [`13`](13-dac-ta-bang-tra-cuu.md) Bảng tra cứu | E4-11, E4-03, E4-04 |
| [`14`](14-phuong-phap-golden-set.md) Golden set | E7-01, E7-02 |
| [`15`](15-phong-van-nguoi-dung.md) Phỏng vấn | Golden set · xác thực [`03`](03-pham-vi-nghiep-vu-v1.md) và P3 |

---

## 7. Ma trận rủi ro → biện pháp

| Rủi ro | Biện pháp trong backlog | Kiểm chứng |
|---|---|---|
| Đồ thị hiệu lực sai | E2-08 + chuyên gia review 100% | 10 câu bẫy pass bằng SQL thuần |
| LLM bịa thuế suất | E4-11 tool calling, không qua RAG | `dieu_kien_ap_dung` NOT NULL |
| LLM khẳng định mã HS | E4-03 guardrail ① | ⚙️ 100% nhóm K |
| Rò rỉ dữ liệu giữa tenant | E5-04 lọc ở tầng DB | ⚙️ required check |
| Credit AWS không như kỳ vọng | E1-02 kiểm chứng ngày 1–2 | Báo cáo PO |
| OCR nuốt hết Sprint 1 | E8-02 timebox 3 ngày | Hết giờ là dừng |
| Streaming Blazor vướng | E8-01 spike Sprint 1 | Báo cáo 1 trang |
| Chuyên gia không đủ thời gian | Cam kết văn bản Sprint 0 | Lịch cố định hàng tuần |

---

## 8. Bảng theo dõi độ phủ

Cập nhật cuối mỗi sprint. Cột "Đạt" là số quy tắc đã có đường truy vết hoàn chỉnh tới chỉ số.

| Sprint | BR có story | BR có cài đặt | BR có test | BR có chỉ số | Độ phủ |
|---|---|---|---|---|---|
| 0 | 11/11 | 0/11 | 0/11 | 0/11 | 0% |
| 1 | 11/11 | ⬜ | ⬜ | ⬜ | ⬜ |
| 2 | 11/11 | ⬜ | ⬜ | ⬜ | ⬜ |
| 3 | 11/11 | ⬜ | ⬜ | ⬜ | ⬜ |

**Mục tiêu cuối Sprint 3: 11/11 quy tắc có đường truy vết hoàn chỉnh.** Bất kỳ quy tắc nào còn trống ở cột "có chỉ số" phải được nêu rõ trong báo cáo Go/No-Go — vì đó là quy tắc chưa ai kiểm chứng.

---

## 9. Cách dùng

| Tình huống | Tra cột nào |
|---|---|
| PO nghiệm thu một story | Story → Test → Chỉ số |
| Developer sắp refactor `retrieval` | Cài đặt → BR bị ảnh hưởng → Test cần chạy |
| Chuyên gia hỏi "hệ thống có đảm bảo X không" | BR → Test → Chỉ số hiện tại |
| Chuẩn bị Go/No-Go | §8 — quy tắc nào chưa có chỉ số |
| Có defect ở production | Triệu chứng → BR → Story → tìm chỗ hở trong test |
