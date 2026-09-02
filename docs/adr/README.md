# Architecture Decision Records

Mỗi ADR một trang: **bối cảnh · quyết định · hệ quả · phương án đã loại bỏ**
([`docs/01`](../01-ke-hoach-prototype-scrum.md) §8.1).

ADR tồn tại để ngăn việc sáu tháng sau có người "tối ưu" bằng cách gỡ bỏ một quyết định
mà họ không biết lý do.

## Đánh số

**001–008 đã được đặt trước** trong `docs/01` §8.1 cho các quyết định nghiệp vụ và kiến
trúc lõi. Đừng dùng lại những số này cho việc khác:

| ADR | Nội dung | Trạng thái |
|---|---|---|
| 001 | Blazor WebAssembly thay vì Blazor Server | chưa viết |
| 002 | Camunda 7 chỉ dùng External Task pattern | chưa viết |
| 003 | 7 microservice và tiêu chí tách thêm | chưa viết |
| 004 | `retrieval` đọc trực tiếp schema `corpus` | chưa viết |
| 005 | Bedrock cho prototype, Claude Platform on AWS cho production | chưa viết |
| 006 | Chunking theo Điều, không fixed-size | chưa viết |
| 007 | Theo dõi hiệu lực ở cấp Điều/Khoản | chưa viết |
| 008 | Corpus không nằm trong git | chưa viết |

Từ **009** trở đi là các quyết định phát sinh khi dựng cấu trúc dự án:

| ADR | Nội dung |
|---|---|
| [009](009-services-json-nguon-su-that.md) | `services.json` là nguồn sự thật duy nhất cho cấu trúc monorepo |
| [010](010-ef-core-so-huu-luoc-do.md) | EF Core sở hữu lược đồ, SQL script là hợp đồng liên ngôn ngữ |
| [011](011-api-code-first.md) | API code-first, và cách kiểm soát lệch hợp đồng |
| [012](012-cach-ly-tenant-va-so-huu-vector.md) | Cách ly tenant bằng global query filter; `retrieval` ghi trực tiếp `vector` |
| [013](013-dinh-tuyen-alb-theo-tien-to.md) | ALB định tuyến theo tiền tố, ứng dụng tự xử lý tiền tố |
| [014](014-khoa-jwt-doi-xung-dung-chung.md) | Khoá JWT đối xứng dùng chung cho prototype, và hạn chuyển sang bất đối xứng |
| [016](016-ai-viet-ddl-cho-schema-vector.md) | Ai viết DDL cho schema `vector` — tách quyền sở hữu dữ liệu khỏi quyền tác giả lược đồ |
