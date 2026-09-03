# Nền chung của bộ skill Scrum — đọc TRƯỚC mọi vai, mọi nghi thức

> **§0 kế thừa.** Mọi phân tích trong bộ skill này đứng trên nền duy vật biện chứng
> Mác — Lênin và tư tưởng Hồ Chí Minh. Bản đầy đủ: `../../../../system/worldview.md`.
> Cụ thể ở đây: mọi vai phải trả lời được **ai được lợi, ai chịu thiệt, ai gánh rủi ro**
> khi quyết định phạm vi, và **cấm tô hồng số liệu tiến độ** — nguyên tắc khách quan (§2)
> đứng trên mọi nhu cầu báo cáo cho đẹp.

## 1. Vì sao bộ skill này tồn tại

`docs/01` và `docs/02` mô tả một đội 11 người với 6 nghi thức. Thực tế: **một người, 60
phút mỗi tối**. Bộ skill này không giả vờ đội đó có thật — nó là cách một người **tự chất
vấn mình bằng mười cái đầu có ranh giới trách nhiệm khác nhau**, và ép mọi việc chảy qua
đúng đường ống SDLC mà `docs/16` cam kết.

Đo bằng số, không bằng cảm giác: tính tới 03/09/2026, hạ tầng ticket dựng từ 16/08 vẫn có
**0 milestone · 0 item trên board · 1 issue mở**. Ma trận truy vết đang truy vết trên giấy.

## 2. Nguồn sự thật — ba tầng, không chồng lấn

| Tầng | Chứa gì | Ai ghi |
|---|---|---|
| **GitHub** (issue · milestone · Project #1) | Backlog, sprint, trạng thái ticket, ước lượng | Nghi thức, qua `gh`, account `Hoang-it` |
| **`scrum/`** trong repo | Sổ riêng từng vai · sổ quyết định và bất đồng | Vai và nghi thức, kèm commit |
| **`docs/`** | Luật: Sprint Goal gốc, DoR/DoD, RACI, backlog nguồn, ma trận truy vết | Chỉ sửa khi PO hoặc BA quyết, có ghi vào sổ quyết định |

**Không chép dữ liệu giữa ba tầng.** Trạng thái ticket đọc từ GitHub, không mirror vào
`scrum/`. Lệch nhau thì **GitHub thắng** cho trạng thái, **`docs/` thắng** cho luật.

## 3. Ánh xạ Scrum → GitHub

| Khái niệm | Vật trên GitHub | Ghi chú |
|---|---|---|
| Sprint | **Milestone `S<tuần ISO>`** — `S36`, `S37`… due Chủ nhật 23:59 | Nguồn sự thật. Field `Iteration` trên board đồng bộ theo, không ngược lại |
| Epic `E1`–`E8` | **Issue cha**, nhãn `type: epic` | Story là **sub-issue** native (API `/issues/{n}/sub_issues` đã kiểm, chạy được) |
| Story | Sub-issue của epic, nhãn `type: story` | Tiêu đề `[Story] E2-01 — <tên>` |
| Cột quy trình | Field `Status`: `Backlog · Todo · In Progress · In Review · Testing · Done` | Điều kiện chuyển cột ở `gates.md` |
| Ước lượng | Field `Story Points` — **Fibonacci như `docs/09`** | Xem §4 |
| Ưu tiên · Thành phần · Chuyên gia | Field `Priority` · `Component` · `Expert Review Required` | Đã có sẵn |

## 4. Sức chứa sprint — chỗ dễ tự lừa nhất

Sprint dài **1 tuần thật = 7 buổi × 60'**. Story point giữ nguyên thang Fibonacci của
`docs/09`, **nhưng thang đó hiệu chỉnh cho đội 7 người toàn thời gian** — nên tới lúc này
**chưa ai biết 1 điểm bằng mấy buổi**.

Luật xử lý, không được lách:

1. `/sprint-planning` cam kết bằng **số buổi ước tính**, không bằng tổng điểm.
2. `/retro` cuối mỗi sprint ghi vào `scrum/decisions.md` **tỷ lệ thực đo: bao nhiêu điểm
   hoàn thành trên bao nhiêu buổi thật**. Sau ba sprint mới có tỷ lệ dùng được.
3. Trước khi có tỷ lệ đó, **cấm mọi vai phát biểu "sprint này ôm nổi X điểm"** — đó là con
   số không có cơ sở, và `worldview.md` §2 gọi đúng tên nó là xuất phát từ mong muốn.

## 5. Trần — thứ giữ cho bộ skill không chết trong ba tuần

| | Trần |
|---|---|
| Gọi một vai | ≤ 1 màn hình · tối đa **1 câu hỏi ngược** |
| Nghi thức | ≤ 2 màn hình · đúng **1 lượt ghi** (GitHub và/hoặc `scrum/`) · tối đa **1 câu hỏi** |
| Ghi lên GitHub | Luôn **đưa bản nháp trước**, gộp thành **một câu duyệt**, rồi ghi loạt |

## 6. Commit — mỗi buổi một dấu vết

Mỗi lần chạy vai hoặc nghi thức có ghi file thì commit ngay, phạm vi hẹp:

```bash
git add scrum/ && git commit -m "scrum(<vai|nghi-thuc>): <việc>"
```

Đây cũng là cách buổi họp được tính vào sàn **≥1 commit nội dung mỗi ngày** của `D-013`.
Không commit thì buổi đó không tồn tại — `git log` là bằng chứng tiến độ duy nhất
(`G-01/CLAUDE.md` §5).
