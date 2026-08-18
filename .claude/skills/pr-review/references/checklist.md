# Checklist review theo đường dẫn — RAG XNK

Danh sách này không thay thế việc đọc diff. Nó ghi lại những chỗ mà **hỏng thì đắt**
và **CI không bắt được**, xếp theo đường dẫn để tra nhanh. Chỉ đọc phần tương ứng với
file mà PR chạm tới.

Nguyên tắc chung khi cân nhắc mức độ: cái gì revert bằng một commit thì để lại comment
không chặn; cái gì làm hỏng thước đo, rò rỉ credential, hay khoá kiến trúc vào một nhà
cung cấp thì chặn.

---

## `eval/gates.yml` · `.github/quality-gates.yml`

Đây là hai file định nghĩa "thế nào là đủ tốt". Ai sửa được chúng thì sửa được kết luận
của mọi phép đo về sau.

- **Ngưỡng đi xuống là cờ đỏ.** `.github/quality-gates.yml` ghi rõ nguyên tắc bánh cóc:
  ngưỡng chỉ được đi lên. Một PR vừa sửa code vừa hạ `coverage_min_line_percent` hay
  `max_relative_drop_percent` gần như luôn là hạ ngưỡng để code của chính nó xanh.
  Hỏi thẳng: nếu giữ nguyên ngưỡng thì PR này đỏ ở đâu?
- **`enabled: false` trong `eval/gates.yml`** đang tắt cổng eval vì bộ đo E7-03/E7-04
  chưa có. Nếu PR bật nó lên, đó là tin tốt — nhưng phải kèm bộ đo thật, không phải
  bật cho có.
- Nới ngưỡng có thể chính đáng. Điều kiện là PR phải nêu lý do trong mô tả, không phải
  giấu trong một diff dài. Không có lý do thì đó là điểm chặn.

## `eval/golden-set/`

`docs/14` gọi việc sửa golden set cho chỉ số đẹp hơn là "gian lận với chính mình" — và
đó là bẫy dễ mắc nhất vì nó không giống hành vi xấu khi nhìn từng dòng diff.

- Câu hỏi phân biệt: golden set đổi vì **hiểu biết nghiệp vụ thay đổi**, hay vì **chỉ
  số đang xấu**? Câu trả lời nằm ở thứ tự thời gian — kiểm tra xem PR này có đang sửa
  code retrieval/prompt cùng lúc không.
- Xoá hoặc sửa câu hỏi có sẵn nặng hơn nhiều so với thêm câu mới. Thêm thì baseline
  phải chạy lại (`invalidate_when_golden_set_changes: true`); sửa thì lịch sử chỉ số
  trước đó mất ý nghĩa mà không ai nhận ra.
- `eval/golden-set/baseline.json` là ngoại lệ: do CI sinh ra, không phải thước đo.
  Thay đổi ở riêng file này không cần soi như trên.

## `prompts/`

Nội dung nghiệp vụ XNK, không phải chuỗi ký tự kỹ thuật.

- Bất kỳ thay đổi nào chạm hiệu lực văn bản, thuế suất, mã HS, Incoterms, loại hình tờ
  khai → **cần chuyên gia XNK xác nhận** trước khi đóng story (`docs/01` §5.2). Nếu PR
  chưa có dấu vết xác nhận đó, đây là điểm chặn về quy trình chứ không phải về code.
- Chuyên gia chỉ có 20% × 2 người. Nếu cần hỏi, yêu cầu tác giả mở issue form
  **Expert Review** với một câu hỏi đóng và 2–3 phương án — đừng đẩy sang họ một câu
  hỏi mở.

## `bpmn/`

- **Java Delegate bị cấm tuyệt đối.** Nó khoá quy trình vào Camunda 7 và JVM. `ci-bpmn`
  chặn, nhưng vẫn đọc diff: một `camunda:class`, `camunda:delegateExpression` hay
  `camunda:expression` gọi bean Java đều là cùng một vấn đề mặc áo khác nhau.
- Chạy được trên máy trước khi tranh luận: `python .github/scripts/validate_bpmn.py bpmn/`

## `.github/workflows/`

`docs/00` §8.5 chốt: không có đường lên production mà không qua người duyệt, **kể cả
khi ai đó sửa được workflow file**. Nên workflow là bề mặt tấn công, không phải file
cấu hình thường.

- `pull_request_target` cộng với checkout code của nhánh đến là lỗ hổng kinh điển: code
  chưa được duyệt chạy với token có quyền ghi. Thấy cặp này là chặn.
- `permissions:` mở rộng — đặc biệt `contents: write`, `id-token: write`, `packages:
  write` — phải tương xứng với việc job thực sự làm. Mặc định là thu hẹp, không phải
  mở rộng.
- Action bên thứ ba nên ghim theo SHA đầy đủ, không phải tag. Tag di chuyển được.
- Secret không được in ra log, kể cả gián tiếp qua `echo` một biến ghép chuỗi.

## `infra/`

- **Cần 2 approval** (`pull_request` trong `.github/quality-gates.yml`), do
  `pr-governance` thực thi và loại review của tác giả.
- **Không thêm access key AWS vào Secrets.** Dự án dùng OIDC; một access key rò rỉ là
  mất cả tài khoản. Đây là một trong ba việc `CONTRIBUTING.md` cấm thẳng.
- Thay đổi chạm environment `production` phải giữ nguyên required reviewers — đó là
  chốt chặn cuối trước khi deploy thật.

## `db/migrations/`

- **Cần 2 approval**, cùng lý do với `infra/`: hỏng thì không revert bằng một commit.
- Migration phá huỷ (drop column, drop table, đổi kiểu làm mất dữ liệu) cần đường lùi
  rõ ràng. Hỏi: nếu deploy xong mới phát hiện sai thì khôi phục bằng cách nào?
- Migration và code đọc/ghi bảng đó nên đi cùng nhau hoặc theo thứ tự tương thích
  ngược, không thì cửa sổ giữa hai lần deploy sẽ lỗi.

## `src/dotnet/`

- `warnings_as_errors: true` và `enforce_format: true` — CI bắt phần này, đừng tốn chỗ
  trong review để nhắc lại. Tập trung vào cái CI không thấy.
- Cách ly tenant (BR-11) có test riêng trong `ci-dotnet`. Bất kỳ truy vấn nào không lọc
  theo tenant đều là lỗi bảo mật dữ liệu, không phải lỗi logic.

## `src/dotnet/Xnk.Web/` (Blazor WASM)

- Ngân sách gói **3500 KB sau Brotli**, cảnh báo từ 85%. Căn cứ là nhân viên tra cứu
  tại cảng dùng mạng di động (`docs/00` §6) — nên khi review một PR thêm thư viện
  client-side, hỏi thẳng phần trăm ngân sách nó ăn.

## `src/python/`

- `ruff check`, `ruff format --check`, `mypy` đều phải sạch — CI lo. Review tập trung
  vào ranh giới module, xử lý lỗi và chỗ nào nuốt exception.

## `docs/`

- `docs/16-ma-tran-truy-vet.md` là ma trận truy vết. Thêm yêu cầu hay đổi phạm vi mà
  không cập nhật nó thì ma trận mục ruỗng dần và không ai phát hiện được thời điểm nó
  bắt đầu sai.
- Quyết định kiến trúc thuộc về `docs/adr/`. Một PR thay đổi hướng kỹ thuật mà không có
  ADR kèm theo là mất dấu vết lý do — sáu tháng sau không ai dựng lại được.

## Mọi PR

- Tiêu đề Conventional Commits: squash merge lấy nó làm dòng changelog vĩnh viễn.
  Tiêu đề đúng cú pháp nhưng vô nghĩa (`fix(ci): sửa lỗi`) vẫn là changelog rác — cú
  pháp thì `pr-governance` bắt, còn nghĩa thì chỉ người review bắt được.
- `Closes #<n>` phải trỏ đúng issue mà PR thực sự đóng.
- Nhánh nên tạo bằng nút "Create a branch" trên issue; tên nhánh không bắt đầu bằng số
  issue thì board không tự chuyển cột.
