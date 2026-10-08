# ADR-012 — Cách ly tenant bằng global query filter, và `retrieval` ghi trực tiếp `vector`

**Trạng thái:** chấp nhận · 2026-08-16

## Bối cảnh

[`docs/00`](../00-ke-hoach-tong-the.md) §12 yêu cầu tường minh: corpus riêng của tenant
cách ly bằng `tenant_id` **áp dụng trong mệnh đề WHERE của chính truy vấn ở tầng
database** — không lọc ở tầng application. Đây là ranh giới bảo mật, bắt buộc có test tự
động, và test đó là required status check.

Phần thứ hai chưa được chốt ở đâu cả: §4.2 cho `retrieval` quyền **đọc** `corpus` và
`vector`, nhưng không nói **ai ghi** vào `vector`.

## Quyết định

### Cách ly tenant

Thực thi bằng **global query filter của EF Core** trên `CorpusDbContext`:

```csharp
entity.HasQueryFilter(d => d.TenantId == null || d.TenantId == CurrentTenantId);
```

Ngữ nghĩa: thấy văn bản dùng chung (`TenantId == null`) cộng văn bản của chính tenant
mình. Không có ngữ cảnh tenant thì **chỉ thấy phần dùng chung**, không phải thấy tất cả.

`CurrentTenantId` phải là thuộc tính của chính `DbContext`, đọc qua `ITenantContext`.

### Sở hữu schema `vector`

**`retrieval` có quyền đọc-ghi trực tiếp `vector`**, và quyền đọc `corpus`.

## Hệ quả

- Mọi truy vấn `Documents` mang bộ lọc, kể cả truy vấn mà người viết quên nghĩ tới nó —
  đó chính là điểm của lựa chọn này.
- Ngữ cảnh tenant thiếu gây **mất dữ liệu nhìn thấy được**, không gây rò rỉ im lặng. Chọn
  nghiêng về phía đó là có chủ đích: lỗi loại một lộ ra trong vài phút, lỗi loại hai có
  thể không bao giờ bị phát hiện.
- `IgnoreQueryFilters()` ở tầng nghiệp vụ là xoá lớp cách ly. Nếu thật sự cần đọc xuyên
  tenant, làm bằng một `DbContext` riêng có tên nói rõ điều đó, để nó hiện ra trong review.
- Bốn test trong `TenantIsolationTests` khoá hành vi này lại, chạy trên PostgreSQL thật.
  Một trong bốn kiểm tra chuỗi SQL sinh ra — ba test kia vẫn xanh nếu ai đó chuyển việc
  lọc lên C#, test đó thì không.
- `retrieval` ghi thẳng `vector` giữ cho lọc hiệu lực và vector search **nằm trong cùng
  một câu SQL**, đúng yêu cầu của §10.3. Cái mất: hai service cùng chạm một cụm bảng, nên
  thay đổi lược đồ `vector` phải phối hợp.
- Extension `pgvector` được tạo trong migration của `corpus` vì đó là migration duy nhất
  hiện có. Extension là phạm vi toàn database; **các bảng** trong schema `vector` vẫn thuộc
  quyền `retrieval`.

## Phương án đã cân nhắc và loại bỏ

**Lọc ở tầng repository/service.** Chỉ cần một chỗ quên gọi hàm lọc là rò rỉ, và chỗ quên
đó không hiện ra trong bất kỳ diff nào. §12 loại bỏ thẳng phương án này.

**Row-Level Security của PostgreSQL.** Mạnh hơn — cách ly nằm ở database, ứng dụng không
đi vòng được. Loại bỏ ở prototype vì đòi mỗi request phải chạy trên một kết nối có
`SET LOCAL` đúng tenant, làm hỏng connection pooling và thêm một lớp phải gỡ khi có sự cố.
**Đáng xem lại trước khi lên production** — khi đó RLS là lớp phòng thủ thứ hai sau global
query filter, không phải thay thế.

**`corpus-service` ghi `vector` qua API.** Một chủ sở hữu ghi duy nhất, ranh giới sạch hơn.
Loại bỏ vì thêm một chặng mạng cho thao tác hàng loạt (sinh embedding cho toàn bộ corpus),
và buộc `corpus-service` phải biết về chiều vector và mô hình embedding — kiến thức thuộc
về `retrieval`.

**`ingestion` ghi `vector`.** Trái với §4.2, vốn mô tả `ingestion` ghi qua API của
`corpus-service`; và mô hình embedding thuộc `retrieval`.
