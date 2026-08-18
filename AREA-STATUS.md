---
area_id: G-01/projects/rag-xnk
health: on-track
last_activity: 2026-08-16
hours_last_7d: null
key_metric: "13 commit trong 7 ngày, nhưng 11 dồn vào một ngày (16/08) — nhịp bùng nổ, không đều"
next_action: "Tiếp tục nhánh e1-11-skeleton-corpus-retrieval; kế hoạch tuần 2 'thu thập tài liệu XNK' còn 0/15"
blockers: []
updated_at: 2026-08-19
---

# Dự án RAG XNK

Mốc số 2 của chặng 1 trong `../../GOAL.md`. Có sản phẩm thật: bốn service FastAPI, skeleton
corpus-service với cách ly tenant ở tầng SQL, Docker cho bốn service, CI chạy migration bằng
ECS one-off task, năm ADR kiến trúc, sổ đăng ký văn bản kèm công cụ thu thập.

**File này nằm trong git repo riêng của rag-xnk** (remote `sa-port-tech/RAG-XNK`), không
phải git của G-01 — vì `projects/rag-xnk/` bị `.gitignore` ở cấp mục tiêu. Nó là đầu ra
tương đương `STATUS.md` nhưng ở cấp mảng, do chính repo con tự quản lý.

## Vấn đề

Nhịp độ dồn cục: 11/13 commit xảy ra trong một ngày duy nhất (16/08), không phải làm đều
suốt tuần. Nhiệm vụ "Thu thập & phân loại tài liệu XNK" của tuần 2 (`../../plan/milestones-2026-08.xlsx`)
tự chấm 0/15 — có hạ tầng nhưng chưa có nội dung.
