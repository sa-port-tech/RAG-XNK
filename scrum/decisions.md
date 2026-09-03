# Sổ quyết định và bất đồng

Mỗi mục có **ngày · vai · loại**. Loại `QUYẾT ĐỊNH` là việc đã chốt; loại `BẤT ĐỒNG` là chỗ
user quyết ngược khuyến nghị của một vai — ghi lại kèm **dự đoán hậu quả đo được**, để
`/retro` sau này đối chiếu ai đúng. Không dùng sổ này để cằn nhằn: cấm nhắc lại một bất
đồng cũ trừ khi số liệu mới chạm đúng dự đoán đã ghi (`.claude/scrum/roles.md` §4).

Quyết định **kiến trúc** không ghi ở đây — chúng đi vào `docs/adr/`.

---

## 2026-09-03 · QUYẾT ĐỊNH · dựng bộ skill Scrum

Mười vai N1–N10 và sáu nghi thức thành skill; backlog và sprint sống trên GitHub; `scrum/`
chỉ giữ sổ vai và sổ này. Ánh xạ: sprint = milestone `S<tuần ISO>` · epic = issue cha ·
story = sub-issue · sáu cột board là cổng pha có điều kiện.

Sức chứa một sprint: **7 buổi × 60'**. Story point giữ thang Fibonacci của `docs/09` — vốn
hiệu chỉnh cho đội 7 người toàn thời gian — nên **tỷ lệ điểm/buổi hiện chưa ai biết**.
`/retro` mỗi sprint phải ghi tỷ lệ thực đo; trước khi có nó, cấm phát biểu "sprint này ôm
nổi X điểm".

Nền đo được lúc dựng: **0 milestone · 0 item trên board · 1 issue mở**, trong khi hạ tầng
ticket đã dựng từ 16/08. Ba tuần, một board rỗng.
