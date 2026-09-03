---
name: sprint-review
description: Chạy buổi Sprint Review của dự án RAG XNK — nghiệm thu increment cuối sprint theo Definition of Done, chấm Sprint Goal đạt hay không, đóng milestone và xử lý carry-over. Dùng khi người dùng nói "chạy sprint review", "đóng sprint", "nghiệm thu sprint", "sprint này xong chưa", "demo cuối tuần", hay khi tới cuối tuần cần chốt kết quả. Buổi này gọi lần lượt /po (nghiệm thu, người nói câu cuối), /tech-lead (tình trạng kỹ thuật) và /expert-xnk (nội dung nghiệp vụ, bắt buộc có mặt nếu increment chạm nghiệp vụ). Nó GHI lên GitHub: đóng milestone, chuyển cột, gắn nhãn carry-over — luôn đưa bản nháp và hỏi một câu duyệt trước khi ghi.
---

# Sprint Review — nghiệm thu, không phải trình chiếu

> **Đọc trước:** `.claude/scrum/README.md` · `roles.md` · `gates.md` · `persona-rules.md` ·
> `github.md`. Luật cao nhất: `../../../../system/worldview.md` §0.

Tham gia (`docs/01` §6): toàn đội + stakeholder + **chuyên gia XNK bắt buộc**. Timebox gốc
1h. Trần ở đây: **≤2 màn hình · 1 lượt ghi · tối đa 1 câu hỏi**.

> **Sprint Review ≠ demo.** `docs/01` §2.3 điểm 4: demo mà không có người biết nghiệp vụ
> ngồi xem thì không phải nghiệm thu. Increment chạm nghiệp vụ mà `/expert-xnk` chưa xác
> nhận thì buổi này **không được kết luận "đạt"**.

## Bước 1 — Đo, trước khi ai phát biểu

```bash
W=$(date +%V)
gh api repos/sa-port-tech/RAG-XNK/milestones --jq '.[] | "\(.title) open=\(.open_issues) closed=\(.closed_issues) due=\(.due_on)"'
gh issue list --milestone "S$W" --state all --limit 50
git log --oneline --since='7 days ago'
gh run list --limit 10
```

Chưa có milestone `S<tuần>` → **không bịa ra sprint đã chạy**. Nói thẳng là sprint này chưa
từng được mở, và đề nghị tạo nó cho đúng hiện trạng trước khi nghiệm thu.

## Bước 2 — Chấm Sprint Goal: đạt hay không đạt

Một câu, không có "đạt một phần". `docs/01` §3.1: PO cam kết với stakeholder về **Sprint
Goal**, không cam kết danh sách story. Nêu kèm số: bao nhiêu story đóng trên bao nhiêu nhận,
bao nhiêu commit nội dung trong tuần, ngày nào 0 commit.

## Bước 3 — `/po` nghiệm thu từng story theo DoD

Với mỗi story đang ở `Testing`, chấm đủ 8 mục DoD (`gates.md` §3). Chú ý hai mục hay bị
lách nhất:

- **Mục 5 — đã deploy được lên môi trường dev.** Chạy được ở máy bạn **không** phải
  increment. Nhánh chưa push thì mục này trượt, không bàn thêm.
- **Mục 7 — PO nghiệm thu trên môi trường dev, không phải máy dev.**

Trượt bất kỳ mục nào → story **không** vào `Done`. Kéo về đúng cột theo `gates.md` §4, gắn
`status: carry-over`, comment lý do kèm bằng chứng.

## Bước 4 — `/tech-lead` và `/expert-xnk` phát biểu trong ô của mình

`/tech-lead`: nợ kỹ thuật phát sinh, ranh giới nào bị vi phạm, có cần ADR không.
`/expert-xnk`: nội dung nghiệp vụ đúng hay sai — **và chỉ điều đó**.

Mỗi vai ≤3 câu. Không trộn giọng, không ai nói thay ai (`roles.md` §3).

## Bước 5 — Bản nháp ghi, một câu duyệt, rồi ghi loạt

In ra đúng thứ sắp ghi, rồi hỏi **một câu duy nhất**:

```
Sắp ghi lên GitHub:
  · đóng milestone S36 (hoặc: giữ mở vì …)
  · #10 → cột <X>, nhãn status: carry-over
  · comment bằng chứng trên #10, #n…
  · story carry-over chuyển sang milestone S37
Duyệt hết?
```

Duyệt xong mới chạy lệnh ở `github.md`. **Không ghi lén, không ghi trước khi hỏi.**

## Bước 6 — Ghi sổ và commit

`scrum/sprint-S<tuần>.md`: Sprint Goal · đạt/không · số đo · story đóng · story carry-over
· ba việc kết luận. Bất đồng → `scrum/decisions.md`.

```bash
git add scrum/ && git commit -m "scrum(sprint-review): S<tuần> — <đạt|không đạt>"
```

## Bước 7 — Một dòng nhắc, không tự chạy

In đúng một dòng: `/retro` chưa chạy · sprint kế tiếp cần `/sprint-planning`. **Không tự
gọi hai skill đó** — Review chấm sản phẩm, Retro mổ quy trình, và `docs/01` §6 để chúng là
hai buổi khác người tham gia.
