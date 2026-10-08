# ADR-010 — EF Core sở hữu lược đồ, nhưng file SQL mới là hợp đồng liên ngôn ngữ

**Trạng thái:** chấp nhận · 2026-08-16

## Bối cảnh

Hệ thống có bốn service .NET và ba service Python dùng chung một cụm PostgreSQL, tách
theo schema ([`docs/00`](../00-ke-hoach-tong-the.md) §4.4). `db/migrations/` được dự kiến
từ đầu nhưng chưa có công cụ nào.

Ràng buộc thật: job `tenant-isolation` trong `ci-dotnet` chạy trên PostgreSQL thật và cần
lược đồ có sẵn; `docker-compose` ở máy dev cũng cần; và ba service Python cần đúng lược đồ
đó mà **không nên phải cài SDK .NET** để có nó.

## Quyết định

**EF Core Migrations sở hữu lược đồ.** Model C# trong `Xnk.Corpus/Data/` là nơi lược đồ
được định nghĩa, và `dotnet ef migrations add` sinh ra migration.

**Nhưng file `.sql` mới là hợp đồng.** Mỗi migration được xuất thành script idempotent và
commit vào `db/migrations/`:

```bash
dotnet ef migrations script --idempotent \
  --project src/dotnet/Xnk.Corpus/Xnk.Corpus.csproj \
  --output db/migrations/000N_ten_migration.sql
```

Mọi bên tiêu thụ khác — service Python, `docker-compose`, task migration trên ECS, công cụ
vận hành — áp file `.sql`, không gọi `dotnet ef`.

Trên môi trường thật, migration chạy bằng **một ECS one-off task trước bước deploy**, do
job `migrate` trong `cd-deploy.yml` điều khiển.

## Hệ quả

- Sinh migration xong **phải** xuất SQL và commit cùng PR. Quên là lược đồ và hợp đồng
  lệch nhau, và không có gì bắt được điều đó tự động ở thời điểm hiện tại.
- `db/apply-migrations.sh` áp toàn bộ thư mục, chạy lại nhiều lần an toàn nhờ cờ
  `--idempotent`.
- Review đọc được thay đổi lược đồ dưới dạng SQL thật trong diff, thay vì phải suy từ
  thay đổi model C#.
- `PostgresFixture` trong test gọi `Database.MigrateAsync()` chứ không `EnsureCreated()`:
  `EnsureCreated` dựng lược đồ thẳng từ model và bỏ qua thư mục migration, nên nó vẫn xanh
  kể cả khi migration hỏng. Nhờ lựa chọn này, chính bộ test cách ly tenant đồng thời là
  phép kiểm chứng rằng migration áp được lên một database trống.

## Phương án đã cân nhắc và loại bỏ

**Chỉ dùng EF Core, không xuất SQL.** Buộc mọi bên tiêu thụ phải có SDK .NET — kể cả ba
service Python và mọi công cụ vận hành. Đổi một lựa chọn nội bộ của phía .NET thành ràng
buộc lên toàn hệ thống.

**Alembic.** Gần `retrieval` và pgvector nhất, nhưng khi đó bốn service .NET phụ thuộc vào
một công cụ Python để chạy được test ở máy.

**Flyway.** Trung lập ngôn ngữ và trưởng thành, nhưng kéo thêm một runtime Java vào CI và
vào máy dev, trong khi dự án đang cố giữ JVM chỉ ở đúng một chỗ là Camunda.

**Chạy `Database.Migrate()` lúc service khởi động.** Không cần hạ tầng gì thêm, làm được
ngay. Nhưng nhiều replica khởi động cùng lúc sẽ tranh nhau, và lỗi migration biến thành
lỗi khởi động khó đọc. Đổi một vấn đề triển khai lấy một vấn đề vận hành lúc 3 giờ sáng.
