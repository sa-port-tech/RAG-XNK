# Cổng SDLC — điều kiện chuyển cột, và bằng chứng phải để lại

Ba thứ user siết (03/09/2026): **truy vết đầu–cuối theo `docs/16`** · **cổng pha giữa sáu
cột** · **ràng branch/PR/commit vào ticket**. File này biến ba điều đó thành điều kiện máy
kiểm được.

## 1. Chuỗi truy vết — thiếu mắt xích là không mở ticket

```
nhu cầu nghiệp vụ (docs/03, docs/15, docs/16)
  → epic E1–E8            (issue cha)
  → story                 (sub-issue, có AC Given/When/Then)
  → test                  (tên test hoặc đường dẫn file test)
  → chỉ số eval           (nếu chạm prompts/ retrieval/ generation/)
  → PR                    (Closes #n)
  → deploy dev            (cd-deploy.yml)
  → nghiệm thu            (PO trên dev; chuyên gia nếu chạm nghiệp vụ)
```

Mở story mà **không nối được lên epic** hoặc **không nói được sẽ kiểm bằng test nào** →
vai từ chối mở, nêu đúng mắt xích còn thiếu.

## 2. Definition of Ready — cổng vào `Todo` (`docs/01` §5.1)

- [ ] Viết theo `Là… tôi muốn… để…`
- [ ] AC dạng Given/When/Then, **kiểm chứng được bằng máy hoặc bằng chuyên gia**
- [ ] Đã có `Story Points`
- [ ] Không phụ thuộc story chưa xong ngoài sprint hiện tại
- [ ] **Chạm nghiệp vụ XNK → `/expert-xnk` đã xác nhận AC đúng nghiệp vụ**

Điều cuối là điều hay bị bỏ nhất và đắt nhất khi bỏ: code đúng theo AC sai nghiệp vụ là
công sức bỏ đi hoàn toàn.

## 3. Definition of Done — cổng vào `Done` (`docs/01` §5.2)

- [ ] PR có ≥1 approval (≥2 nếu chạm `infra/` hoặc `db/migrations/`)
- [ ] Unit test pass, coverage không giảm
- [ ] Toàn bộ CI xanh
- [ ] Chạm `prompts/` `retrieval/` `generation/` → `eval-regression` pass, **Stale Citation Rate = 0**
- [ ] Đã deploy được lên môi trường dev
- [ ] Tài liệu liên quan đã cập nhật (ADR nếu là quyết định kiến trúc)
- [ ] **`/po` nghiệm thu trên môi trường dev, không phải trên máy dev**
- [ ] Chạm nghiệp vụ → `/expert-xnk` xác nhận

## 4. Sáu cột — điều kiện chuyển và bằng chứng

| Từ → tới | Điều kiện | Bằng chứng ghi vào comment ticket |
|---|---|---|
| `Backlog` → `Todo` | DoR đủ 5 mục · đã gán milestone `S<tuần>` | mục DoR nào vừa đạt, ai xác nhận |
| `Todo` → `In Progress` | Có nhánh đặt tên theo ticket | tên nhánh |
| `In Progress` → `In Review` | Có PR mở, thân PR chứa `Closes #n` | số PR |
| `In Review` → `Testing` | CI xanh toàn bộ · có approval | id CI run · người approve |
| `Testing` → `Done` | DoD đủ 8 mục | sha commit merge · kết quả smoke test trên dev |

**Cấm nhảy cóc.** Ticket ở sai cột thì vai **tự kéo về đúng cột**, gắn `status: blocked`
nếu đang chờ thứ khác, và comment lý do kèm bằng chứng. Board phải luôn nói sự thật — nếu
sự thật là "chưa deploy được" thì board phải hiện đúng điều đó.

## 5. Ràng branch / PR / commit vào ticket

| Vật | Quy ước | Ai kiểm |
|---|---|---|
| Nhánh | `<epic>-<số>-<mô tả>` — ví dụ `e1-11-skeleton-corpus-retrieval` | `/daily` |
| PR | Tiêu đề khớp story · thân có `Closes #n` | `pr-governance` + `/tech-lead` |
| Commit | Thuộc về một ticket đang mở | `/daily` nêu **đích danh commit không thuộc ticket nào** |

Commit lạc ticket không phải lỗi nhỏ: nó là công sức không nối được vào chuỗi truy vết ở
§1, tức là công sức mà `docs/16` không chứng minh được đã phục vụ nhu cầu nghiệp vụ nào.
