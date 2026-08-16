# Mục lục tài liệu dự án

## Quy ước trạng thái

| Ký hiệu | Nghĩa |
|---|---|
| ✅ | Hoàn thiện, dùng được |
| 🔶 | **Bản nháp BA — cần chuyên gia XNK xác nhận trước khi dùng làm căn cứ** |
| ⬜ | Khung tài liệu, nội dung điền trong sprint tương ứng |

> ⚠️ **Mọi nội dung gắn 🔶 chưa được kiểm chứng bởi chuyên gia nghiệp vụ.** Không dùng làm căn cứ kỹ thuật hay pháp lý cho tới khi chuyển sang ✅. BA dựng cấu trúc và bản nháp; chuyên gia XNK là người duy nhất có thẩm quyền xác nhận nội dung nghiệp vụ.

---

## Tài liệu quản trị dự án — PO sở hữu

| # | Tài liệu | Trạng thái | Sở hữu |
|---|---|---|---|
| 00 | [Kế hoạch tổng thể](00-ke-hoach-tong-the.md) | ✅ | PO |
| 01 | [Kế hoạch prototype theo Scrum](01-ke-hoach-prototype-scrum.md) | ✅ | PO |
| 02 | [Stakeholder & biên chế nhân sự](02-stakeholder-va-nhan-su.md) | ✅ | PO |

## Tài liệu nghiệp vụ — BA sở hữu

| # | Tài liệu | Trạng thái | Hạn |
|---|---|---|---|
| 03 | [Phạm vi nghiệp vụ v1](03-pham-vi-nghiep-vu-v1.md) | 🔶 | **Sprint 0 — chặn** |
| 04 | [Danh mục văn bản lõi](04-danh-muc-van-ban-loi.md) | 🔶 | **Sprint 0 — chặn** |
| 05 | [Rà soát bản quyền](05-ra-soat-ban-quyen.md) | 🔶 | **Sprint 0 — chặn** |
| 09 | [Product Backlog](09-product-backlog.md) | 🔶 | Sprint 0 |
| 10 | [Đặc tả quy trình BPMN](10-dac-ta-quy-trinh-bpmn.md) | 🔶 | Sprint 0–3 |
| 11 | [Từ điển thuật ngữ & viết tắt XNK](11-tu-dien-thuat-ngu-xnk.md) | 🔶 | Sprint 0–1 |
| 12 | [Ma trận Incoterms 2020](12-ma-tran-incoterms-2020.md) | 🔶 | Sprint 1 |
| 13 | [Đặc tả bảng tra cứu có cấu trúc](13-dac-ta-bang-tra-cuu.md) | 🔶 | Sprint 1–2 |
| 14 | [Phương pháp xây Golden Set](14-phuong-phap-golden-set.md) | ✅ | Sprint 0 |
| 15 | [Kế hoạch phỏng vấn người dùng](15-phong-van-nguoi-dung.md) | ✅ | Sprint 0–1 |
| 16 | [Ma trận truy vết yêu cầu](16-ma-tran-truy-vet.md) | ⬜ | Cập nhật liên tục |

## Tài liệu kỹ thuật — vai trò khác sở hữu

| # | Tài liệu | Trạng thái | Sở hữu | Hạn |
|---|---|---|---|---|
| 06 | Báo cáo spike OCR | ⬜ | Data Engineer | Sprint 1 |
| 07 | Runbook vận hành (AWS) | ⬜ | DevOps | Sprint 3 |
| 08 | Báo cáo Go/No-Go | ⬜ | PO | Tuần 7 |
| 17 | [Runbook GitHub — cài đặt và vận hành](17-runbook-github.md) | ✅ | DevOps + Tech Lead | Sprint 0 |
| 18 | [Dựng lại hạ tầng GitHub từ đầu](18-dung-lai-tu-dau.md) | ✅ | DevOps | Sprint 0 |
| — | [ADR — Architecture Decision Records](adr/) | ⬜ | Tech Lead | Liên tục |
| — | Báo cáo eval mỗi sprint | ⬜ | AI Engineer | Cuối mỗi sprint |

---

## Ba tài liệu chặn Sprint 1

Không có ba tài liệu này ở trạng thái ✅, Sprint 1 không có căn cứ để bắt đầu:

1. **[03 — Phạm vi nghiệp vụ v1](03-pham-vi-nghiep-vu-v1.md)** — cần Business Owner + chuyên gia xác nhận
2. **[04 — Danh mục văn bản lõi](04-danh-muc-van-ban-loi.md)** — cần chuyên gia xác minh số hiệu và trạng thái hiệu lực từ `vbpl.vn`
3. **[05 — Rà soát bản quyền](05-ra-soat-ban-quyen.md)** — cần Pháp chế kết luận

---

## Thứ tự đọc cho người mới vào dự án

1. [`00`](00-ke-hoach-tong-the.md) §1–2 — bối cảnh và ba nguyên tắc bất di bất dịch
2. [`03`](03-pham-vi-nghiep-vu-v1.md) — hệ thống làm gì và không làm gì
3. [`11`](11-tu-dien-thuat-ngu-xnk.md) — thuật ngữ, đọc trước khi họp với chuyên gia
4. [`09`](09-product-backlog.md) — công việc cụ thể
5. [`01`](01-ke-hoach-prototype-scrum.md) — cách làm việc
