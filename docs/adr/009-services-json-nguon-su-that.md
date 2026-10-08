# ADR-009 — `.github/services.json` là nguồn sự thật duy nhất cho cấu trúc monorepo

**Trạng thái:** chấp nhận · 2026-08-16

## Bối cảnh

Khi bắt đầu dựng `src/`, toàn bộ hạ tầng CI/CD đã tồn tại và đang chờ. Bảy workflow đọc
`.github/services.json` để biết: service nào tồn tại, nằm ở đường dẫn nào, project file
tên gì, ECR repository nào, cổng nào, healthcheck ở đâu.

Nghĩa là cấu trúc thư mục **không còn là lựa chọn tự do** ở thời điểm này. Nó đã được
quyết trước, trong một file cấu hình, và mọi workflow đều dựa vào đó.

Câu hỏi cần trả lời: coi file này là hợp đồng ràng buộc, hay coi nó là một bản mô tả có
thể lệch với thực tế rồi chỉnh sau?

## Quyết định

`.github/services.json` là **nguồn sự thật duy nhất**. Cấu trúc thư mục phải khớp nó,
không phải ngược lại.

Thêm hay đổi service là sửa file này **trước**, rồi mới tạo thư mục. Không workflow nào
được viết cứng danh sách service hay đường dẫn.

## Hệ quả

- Đổi tên thư mục service mà quên sửa `services.json` sẽ làm CI đỏ với thông báo trỏ
  thẳng vào file đó. Đây là hành vi mong muốn: sai lệch lộ ra ngay, không âm thầm.
- Mọi thay đổi cấu trúc đi qua một diff một dòng mà CODEOWNERS nhìn thấy được.
- Ba service Python **đều phải tồn tại** kể cả khi mới chỉ làm `retrieval`: `ci-python`
  gom cả ba vào matrix khi PR chạm `src/python/pyproject.toml` hoặc `uv.lock`, rồi thất
  bại nếu thư mục không có. Đây là lý do phía Python có đủ ba skeleton còn phía .NET chỉ
  có `Xnk.Corpus` — bất đối xứng này đến từ logic thật của CI, không phải từ sở thích.
- Ngược lại, `ci-dotnet` chỉ build `Xnk.sln`, nên .NET thêm service theo nhịp cần dùng.

## Phương án đã cân nhắc và loại bỏ

**Viết cứng đường dẫn trong từng workflow.** Bảy workflow × bảy service là 49 chỗ có thể
lệch nhau. Sửa một service phải nhớ sửa bao nhiêu chỗ là câu hỏi không ai trả lời đúng
sau ba tháng.

**Suy ra cấu trúc bằng cách quét thư mục.** Nghe gọn, nhưng khi đó "service tồn tại"
không còn là một quyết định của đội mà là một tác dụng phụ của việc ai đó tạo thư mục.
Thêm nữa, cd-deploy cần thông tin không suy ra được từ tên thư mục — ECR repository,
cổng, đường dẫn healthcheck.
