<!--
  Tiêu đề PR phải theo Conventional Commits — squash merge dùng nó làm dòng changelog.
  Ví dụ: feat(retrieval): lọc chunk theo ngày hiệu lực
  Kiểu hợp lệ: feat · fix · docs · refactor · perf · test · build · ci · chore · revert
-->

## Liên kết issue

<!--
  BẮT BUỘC. Không có dòng này thì workflow pr-governance chặn merge, và issue
  không tự chuyển cột trên Project board (docs/00 §8.3 ④⑥).
-->

Closes #

## Thay đổi cái gì

<!-- 3–5 gạch đầu dòng. Mô tả thay đổi, không mô tả cách đọc diff. -->

-

## Vì sao làm thế này

<!--
  Phần quan trọng nhất của PR. Diff cho biết CÁI GÌ đổi; chỗ này cho biết TẠI SAO.
  Nếu là quyết định kiến trúc, viết ADR trong docs/adr/ và dẫn link ở đây.
-->

## Cách kiểm chứng

<!-- Người review làm gì để tự tin rằng nó chạy đúng. Nêu lệnh cụ thể. -->

-

## Quy tắc nghiệp vụ chạm tới

<!-- Mã BR trong docs/16 §3. Ghi "không" nếu PR thuần kỹ thuật. -->

## Definition of Done

<!-- docs/01 §5.2 — người mở PR tự tích. Ô nào không áp dụng thì ghi "n/a" cạnh ô đó. -->

- [ ] Unit test pass, coverage không giảm
- [ ] Toàn bộ CI xanh
- [ ] Đã tự kiểm chứng trên môi trường dev, không phải chỉ trên máy cá nhân
- [ ] Tài liệu liên quan đã cập nhật (ADR nếu là quyết định kiến trúc)
- [ ] Nếu chạm nghiệp vụ XNK: đã có chuyên gia xác nhận (dẫn link issue Expert Review)

## Cổng bắt buộc theo đường dẫn bị đụng

<!--
  Tích đúng dòng tương ứng với thư mục PR này chạm vào. Các cổng bên dưới do CI
  thực thi — tích ở đây chỉ để người review biết mà chờ đúng check, không thay thế CI.
-->

- [ ] **Chạm `prompts/`, `src/python/retrieval/`, `src/python/generation/` hoặc `eval/`** →
      `eval-regression` phải xanh: `Stale Citation Rate = 0`, `Recall@10 ≥ 0,90`,
      không chỉ số nào tụt quá 2% so với baseline trên `main`
- [ ] **Chạm `infra/` hoặc `db/migrations/`** → cần **≥ 2 approval** (docs/01 §5.2)
- [ ] **Chạm `bpmn/`** → `ci-bpmn` phải xanh: không có Java Delegate, service task phải là external task
- [ ] **Chạm `src/dotnet/`** hoặc schema DB → test cách ly tenant phải pass (BR-11)

## Ảnh chụp màn hình

<!-- Bắt buộc nếu PR đụng src/dotnet/Xnk.Web. Kèm cả bản mobile — nhân viên tra cứu tại cảng dùng điện thoại. -->

## Rủi ro và cách quay lui

<!-- PR này hỏng thì biểu hiện ra sao, và quay lui bằng cách nào. Ghi "revert đơn thuần" nếu đúng vậy. -->
