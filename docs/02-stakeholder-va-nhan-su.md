# Bản đồ Stakeholder & Biên chế nhân sự

> Tài liệu do **Product Owner** sở hữu. Cập nhật khi có thay đổi tổ chức.
> Liên quan: [`00-ke-hoach-tong-the.md`](00-ke-hoach-tong-the.md) · [`01-ke-hoach-prototype-scrum.md`](01-ke-hoach-prototype-scrum.md)

---

# PHẦN I — STAKEHOLDER

## 1. Phân nhóm

```
                        ┌─────────────────────┐
                        │  Sponsor (quyết)    │
                        └──────────┬──────────┘
                                   │
                    ┌──────────────▼──────────────┐
                    │   Steering Committee        │
                    │   (định hướng, gỡ vướng)    │
                    └──────────────┬──────────────┘
                                   │
                        ┌──────────▼──────────┐
                        │   PRODUCT OWNER     │ ◄── kênh duy nhất
                        │   (cầu nối)         │
                        └──────────┬──────────┘
                                   │
        ┌──────────┬───────────────┼───────────────┬──────────┐
        ▼          ▼               ▼               ▼          ▼
   Nghiệp vụ   Công nghệ    Kiểm soát &      Người dùng   Bên ngoài
                             hỗ trợ            cuối
```

**Nguyên tắc bất di bất dịch: Product Owner là kênh giao tiếp duy nhất giữa stakeholder và đội phát triển.** Stakeholder không đặt yêu cầu trực tiếp cho developer, không nhắn riêng cho Tech Lead, không chen việc vào giữa sprint. Mọi yêu cầu đi qua Product Backlog. Đây là điều Scrum Master có nhiệm vụ bảo vệ.

Hai ngoại lệ, đều là thành viên đội ngũ chứ không phải stakeholder ngoài:
- **Chuyên gia nghiệp vụ XNK** làm việc trực tiếp với Data Engineer và AI Engineer
- **Business Analyst** phỏng vấn trực tiếp chuyên gia và người dùng cuối để chi tiết hoá yêu cầu — nhưng **không nhận yêu cầu mới từ stakeholder**, việc đó vẫn qua PO

---

## 2. Nhóm quyết định

| # | Vai trò | Chức danh điển hình | Quan tâm chính | PO cần gì từ họ | Tần suất |
|---|---|---|---|---|---|
| S1 | **Executive Sponsor** | CEO / COO / Giám đốc khối | ROI, rủi ro kinh doanh, uy tín với khách hàng | Phê duyệt ngân sách · **quyết định Go/No-Go** · gỡ vướng cấp tổ chức | Báo cáo cuối mỗi sprint (15') + họp Go/No-Go |
| S2 | **Steering Committee** | Sponsor + Trưởng khối nghiệp vụ + CTO (3–5 người) | Tiến độ, ngân sách, rủi ro liên phòng ban | Định hướng khi có xung đột ưu tiên · phê duyệt thay đổi phạm vi lớn | Cuối mỗi sprint (45') |

**Ghi chú về Steering Committee:** với đội 11 người và 7 tuần, đừng lập một ban chỉ đạo 8 người họp hàng tuần — nó biến thành nghi lễ và làm chậm quyết định. Ba đến năm người, họp cuối sprint, có quyền quyết ngay tại chỗ.

---

## 3. Nhóm nghiệp vụ

| # | Vai trò | Chức danh điển hình | Quan tâm chính | PO cần gì từ họ | Tần suất |
|---|---|---|---|---|---|
| S3 | **Business Owner** | Trưởng phòng XNK / Trưởng bộ phận khai báo hải quan | Hệ thống có giải quyết đúng nỗi đau không | Xác nhận phạm vi nghiệp vụ v1 · cử chuyên gia · nghiệm thu về mặt nghiệp vụ | Sprint Planning + Sprint Review |
| S4 | **Giám đốc kinh doanh / Sales Lead** | GĐ Kinh doanh, Trưởng phòng Sales | Tính năng nào bán được cho khách B2B, định giá | Thông tin thị trường · giới thiệu 1–2 khách hàng làm design partner | 1 lần/sprint (30') |
| S5 | **Trưởng trung tâm đào tạo** | GĐ Trung tâm / Trưởng bộ môn | Học viên dùng được không, có phù hợp giáo trình không | Đại diện nhóm người dùng thứ 3 · phản hồi về nội dung sư phạm | 1 lần/sprint |
| S6 | **Trưởng phòng vận hành / Logistics** | Trưởng phòng Giao nhận | Quy trình thực tế tại cảng/kho, kịch bản dùng trên điện thoại | Xác nhận quy trình XNK có trạng thái (Camunda P3) đúng thực tế | Sprint 3 |

**S3 là stakeholder quan trọng nhất trong nhóm này.** Nếu Business Owner không cam kết cử chuyên gia, dự án không nên khởi động.

---

## 4. Nhóm công nghệ

| # | Vai trò | Chức danh điển hình | Quan tâm chính | PO cần gì từ họ | Tần suất |
|---|---|---|---|---|---|
| S7 | **CTO / Giám đốc CNTT** | CTO, GĐ Công nghệ | Kiến trúc có phù hợp chuẩn công ty không, ai vận hành sau này | Phê duyệt stack · phê duyệt AWS account · cam kết nguồn lực vận hành sau go-live | Sprint 0 + cuối mỗi sprint |
| S8 | **Trưởng nhóm Hạ tầng / IT Operations** | IT Manager, Infra Lead | Ai quản AWS account, VPN, mạng nội bộ; ai trực sự cố | Cấp quyền AWS · cấu hình VPN cho Camunda Cockpit · tiếp nhận runbook | Sprint 0, Sprint 3 |
| S9 | **Enterprise Architect** *(nếu tổ chức có)* | Kiến trúc sư doanh nghiệp | Hòa hợp với bức tranh CNTT tổng thể, tránh trùng lặp hệ thống | Duyệt ADR · chỉ ra hệ thống hiện có cần tích hợp | Sprint 0 + khi có ADR mới |
| S10 | **Security Officer / CISO** | Trưởng phòng ATTT | Cách ly multi-tenant, xử lý dữ liệu khách hàng, quản lý secret | **Duyệt mô hình bảo mật trước Sprint 3** · duyệt OIDC federation · pentest sau prototype | Sprint 0 + Sprint 3 |
| S11 | **Chủ sở hữu hệ thống liền kề** | Quản trị ERP / hệ thống khai báo hải quan hiện có | Tích hợp, đồng bộ dữ liệu, không làm hỏng hệ thống đang chạy | Thông tin về API/dữ liệu có thể dùng lại | Khi cần (Phase 1 trở đi) |

**S10 cần được kéo vào từ Sprint 0, không phải Sprint 3.** Mô hình cách ly tenant là quyết định kiến trúc; nếu CISO bác ở tuần 6, ta phải làm lại schema.

---

## 5. Nhóm kiểm soát & hỗ trợ

| # | Vai trò | Chức danh điển hình | Quan tâm chính | PO cần gì từ họ | Tần suất |
|---|---|---|---|---|---|
| S12 | **Pháp chế** | Trưởng phòng Pháp chế / Luật sư nội bộ | **Bản quyền ICC (Incoterms/UCP)** · điều khoản miễn trừ trách nhiệm · hợp đồng B2B | **Kết luận bản quyền — chặn Sprint 1** · duyệt câu disclaimer cuối mỗi câu trả lời | Sprint 0 (chặn) + khi có thay đổi |
| S13 | **Phụ trách bảo vệ dữ liệu cá nhân** | DPO / Trưởng phòng Tuân thủ | Tuân thủ Nghị định 13/2023/NĐ-CP về bảo vệ dữ liệu cá nhân | Xác định dữ liệu nào là dữ liệu cá nhân · yêu cầu về lưu trữ và xoá · thoả thuận xử lý dữ liệu với khách B2B | Sprint 0 + Sprint 3 |
| S14 | **Tài chính / Kế toán** | Kế toán trưởng, FP&A | Ngân sách, dòng tiền, mô hình chi phí khi scale | Duyệt chi · thiết lập thanh toán AWS/Telerik · mô hình giá cho khách B2B | Sprint 0 + báo cáo hàng tháng |
| S15 | **Mua sắm** | Trưởng phòng Mua sắm | Hợp đồng, đấu thầu, điều khoản nhà cung cấp | Mua license Telerik DevCraft · hợp đồng AWS · hợp đồng cộng tác viên chuyên gia | Sprint 0 + trước khi hết trial Telerik |
| S16 | **Nhân sự** | HRBP | Tuyển dụng, hợp đồng, phân bổ nguồn lực | Tuyển bổ sung nếu thiếu · hợp đồng thuê chuyên gia XNK ngoài | Sprint 0 |

**S12 là điểm chặn cứng.** Không có kết luận bản quyền, đội ngũ không được phép ingest tài liệu ICC — và phần diễn giải Incoterms phải tự biên soạn, việc đó cần thời gian của Business Analyst và chuyên gia.

---

## 6. Người dùng cuối

Ba nhóm tương ứng ba loại tenant. Mỗi nhóm cần **đại diện có tên cụ thể**, không phải "phòng ban X".

| # | Nhóm | Số đại diện cần | Vai trò trong dự án | Tần suất |
|---|---|---|---|---|
| S17 | **Nhân viên khai báo hải quan nội bộ** | 3–5 người | Tham gia Sprint Review · dùng thử tuần 6–7 · báo cáo sai sót · trả lời phỏng vấn của BA | Sprint Review + tuần 6–7 |
| S18 | **Khách hàng B2B (design partner)** | 1–2 tổ chức | Xác nhận nhu cầu thật · dùng thử · cam kết trở thành khách hàng đầu tiên nếu GO | Sprint 1 (phỏng vấn) + tuần 7 (demo) |
| S19 | **Giảng viên & học viên** | 1 giảng viên + 3–5 học viên | Kiểm chứng nhóm người dùng ít kinh nghiệm nhất — nhóm dễ bị câu trả lời sai gây hại nhất | Tuần 6–7 |

**Vì sao S19 quan trọng hơn vẻ ngoài của nó:** người dùng có kinh nghiệm sẽ tự phát hiện câu trả lời sai. Học viên thì không. Nếu hệ thống đủ an toàn cho nhóm này, nó an toàn cho cả ba nhóm.

**Design partner (S18) nên có cam kết bằng văn bản** — kể cả chỉ là thư ngỏ ý. Một prototype có khách hàng thật chờ dùng là lập luận mạnh hơn nhiều tại buổi Go/No-Go.

---

## 7. Bên ngoài

| # | Đối tác | Vai trò | Chi phí | Cách tiếp cận |
|---|---|---|---|---|
| S20 | **AWS Account Manager / Solutions Architect** | Hỗ trợ credit, review kiến trúc, tối ưu chi phí, chương trình hỗ trợ doanh nghiệp | **Miễn phí** | Liên hệ ngay Sprint 0 — họ thường giúp được nhiều hơn dự tính, đặc biệt về credit |
| S21 | **Progress / Telerik** | Tư vấn license, hỗ trợ kỹ thuật Blazor, gia hạn trial | Miễn phí (tiền bán hàng) | Sprint 0, trước khi kích hoạt trial |
| S22 | **Anthropic** | Rate limit, hỗ trợ kỹ thuật khi chuyển sang Claude Platform on AWS | Miễn phí | Phase 1 |
| S23 | **Đơn vị tư vấn pháp lý ngoài** *(nếu pháp chế nội bộ không đủ chuyên môn về bản quyền quốc tế)* | Kết luận về bản quyền ICC | Có phí | Sprint 0 nếu cần |

**S20 là stakeholder bị bỏ quên nhiều nhất.** AWS SA review kiến trúc miễn phí, và họ biết những chương trình credit mà trang web không công bố. Một cuộc gọi ở tuần 1 có thể thay đổi hoàn toàn bài toán ngân sách.

---

## 8. Ma trận Quyền lực / Quan tâm

```
          CAO │  S7 CTO            │  S1 Sponsor
              │  S10 CISO          │  S2 Steering
   Q          │  S12 Pháp chế      │  S3 Business Owner
   U          │                    │
   Y          │  ── GIỮ HÀI LÒNG ──│── HỢP TÁC CHẶT ──
   Ề          │                    │
   N          │  S14 Tài chính     │  S17 NV nội bộ
              │  S15 Mua sắm       │  S18 Khách B2B
              │  S9 EA · S13 DPO   │  S4 Sales
              │  S8 Hạ tầng        │  S5 Đào tạo
              │                    │  S19 Học viên
          THẤP│  ── THEO DÕI ──────│── THÔNG TIN ĐỀU ──
              └────────────────────┴──────────────────►
                 THẤP          QUAN TÂM          CAO
```

| Ô | Chiến lược | Nhịp |
|---|---|---|
| **Hợp tác chặt** (quyền cao, quan tâm cao) | Tham gia trực tiếp, hỏi ý kiến trước khi quyết | Hàng sprint |
| **Giữ hài lòng** (quyền cao, quan tâm thấp) | Báo cáo ngắn gọn, chỉ kéo vào khi cần phê duyệt | Sprint 0 + khi cần |
| **Thông tin đều** (quyền thấp, quan tâm cao) | Demo, thu thập phản hồi, tạo cảm giác đồng sở hữu | Sprint Review |
| **Theo dõi** (quyền thấp, quan tâm thấp) | Bản tin cuối sprint | Cuối sprint |

**Cạm bẫy thường gặp:** dồn hết năng lượng cho ô "Hợp tác chặt" và bỏ quên ô "Giữ hài lòng". CISO và Pháp chế nằm ở ô đó — họ ít quan tâm hàng ngày nhưng có quyền phủ quyết. Bị họ chặn ở tuần 6 tốn kém hơn nhiều so với 30 phút họp ở tuần 1.

---

## 9. Nhịp giao tiếp

| Hoạt động | Ai | Tần suất | Thời lượng |
|---|---|---|---|
| Sprint Review (demo) | Toàn bộ team + S1, S3, S4, S5, S7, S17 | Cuối mỗi sprint | 1h |
| Steering Committee | S2 + PO | Cuối mỗi sprint | 45' |
| Báo cáo Sponsor | S1 + PO | Cuối mỗi sprint | 15' |
| Bản tin cuối sprint (email) | Toàn bộ stakeholder | Cuối mỗi sprint | — |
| Expert Review Session | Chuyên gia + BA + Data Eng + AI Eng | **Hàng tuần** | 1,5h |
| Phỏng vấn người dùng | BA + đại diện S17/S18/S19 | Sprint 1 | 3 × 45' |
| Họp bảo mật | S10 + Tech Lead + DevOps | Sprint 0, Sprint 3 | 1h |
| Họp pháp chế | S12 + PO + BA | Sprint 0 | 1h |
| **Họp Go/No-Go** | S1 (quyết), S2, S3, PO, Tech Lead, chuyên gia | Tuần 7 | 2h |

---

# PHẦN II — NHÂN SỰ

## 10. Đội ngũ Prototype — 11 người, 6,95 FTE

| # | Vai trò | Số người | Mức tham gia | FTE |
|---|---|---|---|---|
| N1 | Product Owner | 1 | 50% | 0,50 |
| N2 | Scrum Master | 1 | 25% | 0,25 |
| N3 | **Business Analyst / Process Analyst** | 1 | 60% | 0,60 |
| N4 | Tech Lead / Kiến trúc sư | 1 | 100% | 1,00 |
| N5 | Data Engineer | 1 | 100% | 1,00 |
| N6 | Backend Engineer (.NET) | 1 | 100% | 1,00 |
| N7 | AI/ML Engineer (Python) | 1 | 100% | 1,00 |
| N8 | Frontend Engineer (Blazor) | 1 | 60% | 0,60 |
| N9 | DevOps / Platform Engineer | 1 | 60% | 0,60 |
| N10 | Chuyên gia nghiệp vụ XNK | **2** | 20% mỗi người | 0,40 |
| | **Tổng** | **11 người** | | **6,95 FTE** |

Trong đó **Scrum Team (Developers)** là 7 người: N3–N9. Nằm trong khoảng khuyến nghị 3–9 của Scrum.

---

## 11. Mô tả từng vai trò

### N1 — Product Owner · 1 người · 50%

**Trách nhiệm**
- Sở hữu và sắp ưu tiên Product Backlog
- **Quyết định làm gì và làm theo thứ tự nào** — không uỷ quyền cho ai
- Duyệt user story và Acceptance Criteria do BA chi tiết hoá
- Nghiệm thu increment trên môi trường dev (không nghiệm thu trên máy dev)
- Là **kênh giao tiếp duy nhất** giữa stakeholder và đội ngũ
- Bảo vệ bốn thứ dưới áp lực tiến độ: thời gian chuyên gia · chất lượng model · chất lượng golden set · sự có mặt của chuyên gia trong Sprint Review
- Chuẩn bị và trình bày báo cáo Go/No-Go

**Yêu cầu**
- Hiểu nghiệp vụ XNK đủ để đối thoại ngang hàng với chuyên gia (không cần là chuyên gia)
- Hiểu công nghệ đủ để đánh giá đánh đổi kỹ thuật, không bị "kỹ thuật hóa" khi ra quyết định
- Có thẩm quyền thực sự để nói *không* với stakeholder

**Không được kiêm nhiệm với Tech Lead.** PO tối ưu giá trị sản phẩm; Tech Lead tối ưu chất lượng kỹ thuật. Hai mục tiêu này xung đột lành mạnh và cần hai người.

---

### N2 — Scrum Master · 1 người · 25%

**Trách nhiệm**
- Điều phối 5 nghi thức + Expert Review Session
- Gỡ impediment (đặc biệt: chuyên gia bận, quyền AWS chưa cấp, stakeholder chen việc)
- **Chặn scope creep giữa sprint** — yêu cầu mới đi vào Backlog Phase 1
- Theo dõi sức khỏe đội ngũ, chạy Retrospective có giá trị
- Bảo vệ nguyên tắc "PO là kênh duy nhất" **và ranh giới PO/BA** (§11 · N3)

**Yêu cầu**
- Có kinh nghiệm SM thực chiến, không phải người vừa học chứng chỉ
- **Không kiêm Tech Lead** — Tech Lead có động cơ bảo vệ quyết định kỹ thuật của mình; SM phải tạo không gian để đội chất vấn chính những quyết định đó

**Nếu tổ chức không có SM rảnh:** mượn SM từ team khác 25% thời gian. Rẻ hơn nhiều so với cái giá của một Retrospective vô nghĩa.

---

### N3 — Business Analyst / Process Analyst · 1 người · 60%

**Trách nhiệm**
- **Chi tiết hoá 55–70 story**: biến nhu cầu nghiệp vụ thành AC dạng Given/When/Then máy kiểm chứng được
- **Mô hình hoá BPMN quy trình Camunda P1** (và P2–P4 ở production)
- Soạn ba tài liệu chặn Sprint 0: phạm vi nghiệp vụ v1 · danh mục 15 văn bản lõi · hỗ trợ hồ sơ rà soát bản quyền
- **Tự biên soạn ma trận Incoterms 2020** — 11 điều kiện × 5 chiều trách nhiệm (điểm chuyển rủi ro, chi phí vận chuyển, bảo hiểm, thông quan XK, thông quan NK). Bắt buộc tự viết vì bản quyền ICC
- Xây **từ điển viết tắt XNK** (~300 mục) dùng cho query expansion
- Đặc tả bảng structured lookup: `ma_loai_hinh`, `nghiep_vu_vnaccs`
- Hỗ trợ chuyên gia biến kiến thức thành golden set có cấu trúc
- Phỏng vấn người dùng cuối (S17, S18, S19)
- Truy vết yêu cầu: nhu cầu nghiệp vụ → story → test → chỉ số eval

**Yêu cầu**
- **Thành thạo BPMN 2.0** — đây là yêu cầu không thương lượng, xem ghi chú dưới
- Kinh nghiệm viết đặc tả kiểm chứng được (Given/When/Then, không phải đặc tả văn xuôi)
- Có nền hoặc học nhanh được nghiệp vụ XNK / logistics
- Biết phỏng vấn chuyên gia và trích xuất tri thức ngầm

**Vì sao vai trò này cần thiết — lỗ hổng năng lực, không phải lỗ hổng thời gian**

Dự án chọn Camunda, nghĩa là phải có người vẽ được quy trình P1 đúng cả về nghiệp vụ lẫn ký pháp BPMN. Nhìn quanh đội ngũ:

| | Biết quy trình XNK | Biết BPMN | Còn dung lượng |
|---|---|---|---|
| Chuyên gia XNK (20%) | ✅ | ❌ | ❌ |
| Backend .NET | ❌ | ✅ (mức API) | ❌ |
| Tech Lead | ❌ | ✅ | ❌ |
| PO (50%) | một phần | một phần | ❌ |

Không ai có đủ cả ba. Tăng giờ cho những người hiện có cũng không lấp được — đó là lý do nâng PO lên 100% *không* thay thế được vai trò này.

**Ranh giới PO / BA — quy tắc cứng**

| Việc | PO | BA |
|---|---|---|
| Quyết định làm gì, ưu tiên gì | ✅ | ❌ |
| Nghiệm thu increment | ✅ | ❌ |
| Quan hệ stakeholder, nhận yêu cầu mới | ✅ | hỗ trợ |
| Trình bày Go/No-Go | ✅ | cung cấp dữ liệu |
| Chi tiết hoá story, viết AC | duyệt | ✅ |
| Mô hình hoá BPMN | duyệt | ✅ |
| Tài liệu nghiệp vụ (phạm vi, danh mục VB, Incoterms, từ điển) | duyệt | ✅ |
| Hỗ trợ chuyên gia xây golden set | | ✅ |
| Truy vết yêu cầu | | ✅ |

**BA không có quyền quyết định ưu tiên và không nghiệm thu increment.** Nếu BA bắt đầu quyết "cái gì làm trước", vai trò PO đã bị rỗng ruột — đó là anti-pattern kinh điển của mô hình PO + BA, và nó phòng được bằng ranh giới rõ ràng chứ không cần bằng cách bỏ vai trò. Scrum Master theo dõi ranh giới này trong Retrospective.

**Phân bổ:** 100% Sprint 0 (ba tài liệu chặn + backlog Sprint 1), 60% Sprint 1–2, 80% Sprint 3 (BPMN P1 + hỗ trợ chấm điểm). Trung bình 60%.

---

### N4 — Tech Lead / Kiến trúc sư giải pháp · 1 người · 100%

**Trách nhiệm**
- Sở hữu kiến trúc tổng thể; viết và duy trì ADR
- Quyết định kỹ thuật cuối cùng khi đội bất đồng
- Code review cho phần .NET và các API contract giữa service
- Đảm bảo ranh giới 7 microservice không bị xói mòn
- Kiêm phát triển .NET (~50% thời gian viết code)

**Yêu cầu**
- .NET 9 / ASP.NET Core sâu
- Kinh nghiệm thiết kế microservices thật (đã từng chịu hậu quả của việc chia sai)
- AWS ở mức thiết kế, không cần vận hành
- Đã dẫn dắt kỹ thuật ít nhất một dự án hoàn chỉnh

---

### N5 — Data Engineer · 1 người · 100% — **vị trí quan trọng nhất**

**Trách nhiệm**
- Parser văn bản (PDF/DOCX) và **structural parser** phân rã Phần/Chương/Mục/Điều/Khoản/Điểm
- **Đồ thị hiệu lực** — hạng mục rủi ro cao nhất toàn dự án
- Schema PostgreSQL, migration, hàm `hieu_luc_tai_ngay()`
- Trích xuất metadata hiệu lực và quan hệ sửa đổi
- Đối chiếu chất lượng dữ liệu cùng chuyên gia XNK
- Spike OCR

**Yêu cầu**
- Python sâu, PostgreSQL sâu (không chỉ ORM — phải viết được SQL phức tạp)
- Kinh nghiệm xử lý văn bản có cấu trúc, regex ở mức thành thạo
- **Tính tỉ mỉ với dữ liệu** — phẩm chất quan trọng hơn cả kỹ năng. Sai một quan hệ sửa đổi là sai vĩnh viễn mọi câu trả lời sau đó
- Kiên nhẫn ngồi cùng chuyên gia nghiệp vụ hàng tuần

**Vì sao đây là vị trí quan trọng nhất:** nếu đồ thị hiệu lực sai, mọi tầng phía trên đều vô nghĩa — retrieval tốt đến mấy cũng trả về nội dung sai, generation đẹp đến mấy cũng trích dẫn sai. Nếu chỉ tuyển được một người giỏi, tuyển vào vị trí này.

---

### N6 — Backend Engineer (.NET) · 1 người · 100%

**Trách nhiệm**
- `identity-tenant` — xác thực JWT, tổ chức, RBAC, **cách ly `tenant_id`**
- `chat-orchestrator` — vòng đời hội thoại, streaming SSE, điều phối service
- `corpus-service` — CRUD văn bản, API tra cứu có cấu trúc
- `workflow-worker` — External Task Worker cho Camunda
- API contract giữa các service

**Yêu cầu**
- .NET 9, ASP.NET Core, EF Core
- Kinh nghiệm SSE hoặc streaming HTTP
- Hiểu multi-tenancy ở mức bảo mật, không chỉ ở mức tính năng

---

### N7 — AI/ML Engineer (Python) · 1 người · 100%

**Trách nhiệm**
- `retrieval` — embedding BGE-M3, hybrid search BM25 + vector, **bộ lọc hiệu lực**, reranker
- `generation` — tích hợp Claude, Citations API, prompt caching, tool calling
- **Guardrails XNK** ở tầng post-processing
- Bộ eval và toàn bộ chỉ số chất lượng
- Đánh giá và lựa chọn model embedding trên golden set

**Yêu cầu**
- Python, FastAPI
- Kinh nghiệm RAG thực chiến (đã từng đo và cải thiện chất lượng retrieval, không chỉ dựng demo)
- Hiểu embedding, reranking, đánh giá chất lượng
- Kinh nghiệm với LLM API ở mức sản xuất: streaming, tool calling, xử lý lỗi, kiểm soát chi phí
- **Có tư duy đo lường** — biết phân biệt "trông có vẻ tốt hơn" với "tốt hơn theo số liệu"

---

### N8 — Frontend Engineer (Blazor) · 1 người · 60%

**Trách nhiệm**
- Blazor WebAssembly + Telerik UI
- **Chat streaming qua SSE** — dựng prototype ngay Sprint 1, không để tới cuối
- Panel văn bản gốc: chip trích dẫn bấm được, cuộn tới và highlight đúng đoạn
- Badge trạng thái hiệu lực (xanh/vàng/đỏ)
- Màn hình admin và hàng đợi review cho chuyên gia
- Nút "Báo cáo sai sót"
- Responsive cho điện thoại

**Yêu cầu**
- Blazor WebAssembly (không phải chỉ Blazor Server), C#
- Telerik UI for Blazor hoặc thư viện component tương đương
- Cảm nhận UX tốt — giao diện này chuyên gia XNK phải dùng hàng ngày

**Phân bổ:** 30% tuần 1–3 (dựng khung + spike streaming), 100% tuần 4–7.

---

### N9 — DevOps / Platform Engineer · 1 người · 60%

**Trách nhiệm**
- AWS CDK (C#) — toàn bộ hạ tầng dưới dạng mã
- CI/CD GitHub Actions: 7 workflow, path filter, required status checks
- **OIDC federation** GitHub → AWS (không dùng access key)
- Kiểm soát chi phí: budget alarm, tag, lịch tắt tài nguyên ngoài giờ, Cost Anomaly Detection
- Observability: CloudWatch + OpenTelemetry → Jaeger
- Runbook vận hành

**Yêu cầu**
- AWS ở mức vận hành: ECS Fargate, RDS, VPC, IAM
- AWS CDK, ưu tiên C# (cùng ngôn ngữ với team .NET)
- GitHub Actions, Docker
- **Ý thức về chi phí** — biết NAT Gateway là bẫy, biết Fargate không có free tier

**Phân bổ:** 100% tuần 1–2 (dựng nền), 40% tuần 3–7.

---

### N10 — Chuyên gia nghiệp vụ XNK · **2 người** · 20% mỗi người

**Trách nhiệm**
- Xây dựng **golden set** — tài sản giá trị nhất của dự án (cùng BA)
- Review **đồ thị hiệu lực** và danh sách Điều bị sửa đổi (review 100%, không lấy mẫu)
- Đối chiếu bảng biểu thuế và danh mục HS với bản gốc
- **Chấm điểm chất lượng đầu ra** thang 1–5 — chỉ số quyết định Go/No-Go
- Xác nhận AC của mọi story chạm nghiệp vụ (điều kiện Definition of Ready)
- Cung cấp tri thức nghiệp vụ cho BA khi mô hình hoá BPMN và biên soạn ma trận Incoterms
- Xử lý User Task trong quy trình Camunda P1
- Duyệt nội dung trong `prompts/` và `eval/golden-set/` qua CODEOWNERS

**Yêu cầu**
- Khai báo hải quan thực chiến ≥ 5 năm, **hoặc** giảng viên XNK có kinh nghiệm thực tế
- Am hiểu văn bản pháp luật: biết tra cứu hiệu lực, biết đọc quan hệ sửa đổi
- Sẵn sàng làm việc chi tiết với dữ liệu, không chỉ tư vấn ở mức khái niệm

**Vì sao phải là 2 người, không phải 1:**
1. **Chống điểm hỏng đơn** — một người nghỉ là dự án đứng
2. **Đối chứng lẫn nhau** — nghiệp vụ XNK có nhiều điểm hai chuyên gia hiểu khác nhau. Chính những chỗ bất đồng đó là chỗ hệ thống dễ sai nhất và cần được ghi nhận rõ
3. **20% mỗi người dễ xin hơn 40% một người** — họ vẫn giữ công việc chính

**Có thể là người ngoài (cộng tác viên)** nếu nội bộ không có. Chi phí thuê thấp hơn nhiều so với rủi ro không có ai xác nhận đầu ra.

**Có BA rồi vẫn cần đủ 2 chuyên gia.** BA chuyển tri thức thành tài liệu; chuyên gia là nguồn của tri thức đó và là người duy nhất có thẩm quyền nói câu trả lời đúng hay sai. Hai vai trò bổ sung nhau, không thay thế nhau.

---

## 12. Bổ sung khi lên Production

| # | Vai trò | Số người | Bắt đầu từ | Vì sao prototype không cần |
|---|---|---|---|---|
| N11 | **QA / Test Engineer** | 1 (100%) | Phase 2 | Prototype dùng eval automation làm QA chính; production cần integration test, load test, test cách ly tenant chuyên sâu |
| N12 | **UX/UI Designer** | 1 (50%) | Phase 4 | Prototype dùng Telerik mặc định để tập trung chứng minh kiến trúc; production cần nghiên cứu người dùng với cả 3 nhóm |
| N13 | **Backend Engineer (.NET) thứ 2** | 1 (100%) | Phase 3 | Khối lượng service nghiệp vụ tăng: SSO, quota, billing, admin |
| N14 | **Technical Writer** | 1 (30%) | Phase 4 | Tài liệu người dùng, hướng dẫn onboarding tenant B2B, tài liệu đào tạo |
| N15 | **SRE / Trực vận hành** | 1 (50%) | Sau go-live | Trực sự cố, giám sát SLA |
| N16 | **Chuyên gia XNK thứ 3** | 1 (20%) | Phase 1 | Mở rộng golden set từ 50 lên 300–500 câu và review corpus 150 văn bản cần thêm giờ chuyên gia |

**BA nâng lên 100% từ Phase 1** — khối lượng tăng mạnh: mô hình hoá BPMN P2/P3/P4, đặc tả SSO và onboarding tenant, mở rộng từ điển và ma trận nghiệp vụ, hỗ trợ golden set 300–500 câu.

**Biên chế đỉnh ở Phase 4: khoảng 15–16 người**, tương đương ~11 FTE.

---

## 13. Những vai trò KHÔNG cần

Liệt kê để chống phình biên chế — mỗi vai trò thêm vào làm chậm giao tiếp:

| Vai trò | Vì sao không cần |
|---|---|
| **Project Manager riêng** | PO + Scrum Master đã đủ ở quy mô này. Thêm PM tạo tầng giao tiếp thứ ba và làm mờ trách nhiệm |
| **BA thứ hai** | Một BA 60% đủ cho 55–70 story ở prototype. Nâng lên 100% ở Phase 1 trước khi nghĩ tới người thứ hai |
| **DBA riêng** | RDS là dịch vụ quản lý; Data Engineer đủ sức. Cân nhắc lại khi corpus vượt vài triệu chunk |
| **Kiến trúc sư riêng cho từng tầng** | Một Tech Lead cho toàn hệ thống. Chia kiến trúc theo tầng ở quy mô này tạo ranh giới giả tạo |
| **Prompt Engineer riêng** | Đây là công việc của AI Engineer, gắn liền với eval. Tách ra khiến người viết prompt không chịu trách nhiệm về số liệu |
| **Đội kiểm thử thủ công** | Eval automation + chuyên gia chấm điểm hiệu quả hơn nhiều cho loại sản phẩm này |

---

## 14. Rủi ro nhân sự

| Rủi ro | Điểm hỏng đơn | Biện pháp |
|---|---|---|
| **Data Engineer nghỉ giữa chừng** | Đồ thị hiệu lực là kiến thức tập trung ở một người | Pair programming với AI Engineer ở phần schema · ADR-007 ghi lại tư duy thiết kế · code review bắt buộc bởi Tech Lead |
| **Chuyên gia XNK bị rút về việc chính** | Đã giảm thiểu bằng cách dùng 2 người | Cam kết bằng văn bản có chữ ký Business Owner · lịch cố định hàng tuần · báo cáo lên Steering nếu bị vi phạm |
| **BA lấn sang quyết định ưu tiên** | Rỗng ruột vai trò PO | Ranh giới ở §11 · N3 · Scrum Master theo dõi trong Retrospective · PO là người duy nhất nghiệm thu |
| **Không tuyển được BA biết BPMN** | Quy trình Camunda P1 không có ai mô hình hoá | Phương án dự phòng: thuê ngoài 2–3 buổi workshop mô hình hoá với chuyên gia BPMN, BA nội bộ tiếp quản duy trì |
| **DevOps giữ hết kiến thức AWS trong đầu** | Không ai deploy được khi họ nghỉ | **Toàn bộ hạ tầng bằng CDK trong git** — đây chính là biện pháp giảm thiểu · runbook viết từ Sprint 3 |
| **Tech Lead quá tải vì kiêm cả code lẫn kiến trúc** | Trở thành nút cổ chai review | Giới hạn code ở ~50% thời gian · phân quyền review .NET cho N6 sau Sprint 1 |
| **Frontend vào muộn, dồn việc cuối** | Sprint 3 quá tải | Spike streaming SSE ngay Sprint 1 để phát hiện vướng sớm · N8 tham gia 30% từ đầu |

---

## 15. Tóm tắt biên chế

| | Người | FTE |
|---|---|---|
| **Prototype (7 tuần)** | **11** | **6,95** |
| Production Phase 1–3 | 12–13 | ~9 |
| Production Phase 4 (đỉnh) | 15–16 | ~11 |
| Vận hành sau go-live | 5–6 | ~3,5 |

**Stakeholder cần quản lý: 23 (S1–S23)**, trong đó 8 người thuộc nhóm cần tương tác hàng sprint.
