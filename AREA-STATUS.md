---
area_id: G-01/projects/rag-xnk
health: stalled
last_activity: 2026-08-18
hours_last_7d: 0.00
key_metric: "0 commit NỘI DUNG trong 15 ngày (18/08 → 02/09); sàn D-013 ≥1 commit/ngày đạt 0/2; corpus 0/15 văn bản tải về, 16/16 bản ghi còn ở de_xuat"
next_action: "Xác minh ≥3 văn bản nhóm A trên vbpl.vn → đổi trang_thai_thu_thap sang da_xac_minh → chạy tools/corpus/thu_thap.py → commit thật trong khối 11:00–12:00 hôm nay"
blockers: []
updated_at: 2026-09-02
---

# Dự án RAG XNK

Mốc số 2 của chặng 1 trong `../../GOAL.md`. Hạ tầng có thật: bốn service FastAPI, skeleton
corpus-service cách ly tenant ở tầng SQL, Docker cho bốn service, CI chạy migration bằng ECS
one-off task, năm ADR, sổ đăng ký văn bản kèm hai công cụ (`thu_thap.py`, `kiem_tra.py`).

**File này nằm trong git repo riêng của rag-xnk** (remote `sa-port-tech/RAG-XNK`), không phải
git của G-01 — vì `projects/rag-xnk/` bị `.gitignore` ở cấp mục tiêu. Đo nhịp độ phải chạy
`git -C projects/rag-xnk log`.

## Vì sao chấm `stalled` chứ không phải `at-risk`

Repo có commit gần nhất ngày **31/08** (`7ca6c78`), tức trong 14 ngày — đọc theo mặt chữ §6
thì chưa `stalled`. Nhưng commit đó sửa đúng **một file**: `.claude/skills/pr-review/SKILL.md`,
tức là chép lại chính chỉ đạo D-013 vào skill. Đó là việc **nói về dự án**, không phải việc
**làm dự án**. Commit nội dung cuối cùng là `d726562` + `e5761d3` ngày **18/08** — cách hôm nay
**15 ngày**, vượt ngưỡng 14 ngày của `../../CLAUDE.md` §6.

Chuẩn này áp cho cả tương lai: commit chỉ đụng file skill, file trạng thái, file kế hoạch
**không tính** vào sàn ≥1 commit/ngày của D-013. Bản cập nhật AREA-STATUS.md này cũng không
tính.

## Số liệu đối chiếu D-013

| Kiểm cái gì | Chỉ tiêu | Thực đo 02/09 |
|---|---|---|
| Commit nội dung/ngày | ≥1 (D-013 QĐ 1) | **0/2 ngày** (31/08, 01/09) |
| Commit tuần 36 | ≥7 | **0** — mốc `/review` 06/09 ghi rõ: 0 là **lần chết thứ ba** |
| Giờ thật 7 ngày | 7.00h/tuần (60'×7) | **0.00h** thật / 2.50h đã xếp lịch |
| Văn bản tải về | 15 | **0** — `corpus/raw/` và `corpus/derived/` rỗng |
| Bản ghi registry | 15 + ứng viên | 16 bản ghi, **16 ở `de_xuat`**, 0 ở `da_xac_minh` |

Khối 60' được cấp từ 31/08 vì D-013 nhận rằng 15' không đủ để viết một lát script rồi chạy thử
rồi commit. Đã cấp đủ giờ hai ngày, sản lượng vẫn là **0 dòng code**. Ăn cỗ đi trước, lội nước
theo sau — mà đây thì cỗ đã dọn sẵn hai mâm, chưa ai ngồi vào.

## Chỗ nghẽn thật, không phải blocker

`thu_thap.py` chỉ tải bản ghi từ `da_xac_minh` trở lên. Cả 16 bản ghi còn ở `de_xuat`, nên chạy
script bây giờ tải về **0 file** — không phải script hỏng, mà là **bước xác minh chưa ai làm**.
Bước đó thuộc thẩm quyền BA (điền số hiệu, ngày ban hành, nguồn tra), khác với `trang_thai`
hiệu lực và `quan_he_sua_doi` — hai thứ đó `corpus/registry/van-ban.yaml` đã chốt là của chuyên
gia đối chiếu vbpl.vn. Vậy **không có blocker ngoại cảnh nào**: việc chưa làm, chỉ là chưa làm.

## Nhánh và trạng thái remote

Đang ở `e1-11-skeleton-corpus-retrieval`, cây làm việc **sạch**, không có gì dở dang chờ commit.
`origin/main` dừng ở `fc40ad3`. Ba nhánh cũ (`3-tai-lieu-dung-lai-tu-dau`, `5-tat-bootstrap-bypass`,
`7-bao-cao-eval-tu-mau-thuan`) chưa dọn.
