# Architecture Decision Records

Mỗi ADR một trang: **bối cảnh · quyết định · hệ quả · phương án đã loại bỏ**
([`docs/01`](../01-ke-hoach-prototype-scrum.md) §8.1).

ADR tồn tại để ngăn việc sáu tháng sau có người "tối ưu" bằng cách gỡ bỏ một quyết định
mà họ không biết lý do.

## Đánh số

**Số hiệu có ĐÚNG BA chữ số.** `ADR-012`, không phải `ADR-0012`. Ghi bốn chữ số là trỏ vào
một file không tồn tại, và không có gì báo lỗi — mã vẫn biên dịch, CI vẫn xanh, chỉ có người
đọc sáu tháng sau là đi tìm một tài liệu chưa từng có. Ngày 03/09/2026 review đếm được **11
trích dẫn bốn chữ số trong 9 file**, mỗi cái còn sai luôn cả chủ đề so với bảng dưới đây; sửa
hết ở `#11`. Kiểm nhanh trước khi mở PR:

```bash
grep -rn "ADR-00[0-9][0-9]" src db tools .github --exclude-dir={bin,obj}   # phải rỗng
```

**001–008 đã được đặt trước** trong `docs/01` §8.1 cho các quyết định nghiệp vụ và kiến
trúc lõi. Đừng dùng lại những số này cho việc khác:

| ADR | Nội dung | Trạng thái |
|---|---|---|
| [001](001-blazor-webassembly-thay-vi-blazor-server.md) | Blazor WebAssembly thay vì Blazor Server | ✅ viết 09/09/2026 |
| [002](002-camunda-chi-dung-external-task.md) | Camunda 7 chỉ dùng External Task pattern | ✅ viết 09/09/2026 |
| [003](003-bay-microservice-va-tieu-chi-tach-them.md) | 7 microservice và tiêu chí tách thêm | ✅ viết 09/09/2026 |
| 004 | `retrieval` đọc trực tiếp schema `corpus` | ⛔ **không viết nữa** — đã quyết ở [ADR-012](012-cach-ly-tenant-va-so-huu-vector.md) |
| 005 | Bedrock cho prototype, Claude Platform on AWS cho production | ⛔ **không viết nữa** — đã quyết ở [ADR-015](015-giao-thuc-tuong-thich-openai.md) |
| 006 | Chunking theo Điều, không fixed-size | ⏳ **chưa quyết** — quyết định nghiệp vụ, chờ epic E2 |
| 007 | Theo dõi hiệu lực ở cấp Điều/Khoản | ⏳ **chưa quyết** — quyết định nghiệp vụ, chờ epic E3 |
| [008](008-corpus-khong-nam-trong-git.md) | Corpus không nằm trong git | ✅ viết 09/09/2026 |

Hai dòng ⛔ là hai quyết định **đã được ra rồi, dưới số khác**. Giữ số 004 và 005 trống thay vì
viết lại nội dung ở đó: hai ADR cùng sở hữu một quyết định thì phiên bản nào là thật trở thành
câu hỏi, và câu hỏi đó luôn được hỏi vào lúc tệ nhất.

Hai dòng ⏳ là quyết định **chưa ai ra**. Chúng ở đây để giữ chỗ, không phải để ai đó điền cho
đủ bảng — viết một ADR cho một quyết định chưa có là biến phỏng đoán thành thứ trông như căn cứ.

Từ **009** trở đi là các quyết định phát sinh khi dựng cấu trúc dự án:

| ADR | Nội dung |
|---|---|
| [009](009-services-json-nguon-su-that.md) | `services.json` là nguồn sự thật duy nhất cho cấu trúc monorepo |
| [010](010-ef-core-so-huu-luoc-do.md) | EF Core sở hữu lược đồ, SQL script là hợp đồng liên ngôn ngữ |
| [011](011-api-code-first.md) | API code-first, và cách kiểm soát lệch hợp đồng |
| [012](012-cach-ly-tenant-va-so-huu-vector.md) | Cách ly tenant bằng global query filter; `retrieval` ghi trực tiếp `vector` |
| [013](013-dinh-tuyen-alb-theo-tien-to.md) | ALB định tuyến theo tiền tố, ứng dụng tự xử lý tiền tố |
| [014](014-khoa-jwt-doi-xung-dung-chung.md) | Khoá JWT đối xứng dùng chung cho prototype, và hạn chuyển sang bất đối xứng |
| [015](015-giao-thuc-tuong-thich-openai.md) | Gọi mô hình ngôn ngữ qua giao thức tương thích OpenAI — local Ollama, cloud Bedrock |
| [016](016-ai-viet-ddl-cho-schema-vector.md) | Ai viết DDL cho schema `vector` — tách quyền sở hữu dữ liệu khỏi quyền tác giả lược đồ |
