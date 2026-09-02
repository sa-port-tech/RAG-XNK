# ADR-016 — Ai viết DDL cho schema `vector`, và vì sao nó chưa tồn tại

**Trạng thái:** chấp nhận · 2026-09-02

## Bối cảnh

Hai ADR đang nói hai điều mà ghép lại thì không thi hành được:

* [ADR-010](010-ef-core-so-huu-luoc-do.md): **EF Core sở hữu lược đồ**, mọi migration sinh
  từ model C# rồi xuất ra `db/migrations/*.sql`. Phương án Alembic đã bị loại vì khi đó bốn
  service .NET phải phụ thuộc vào một công cụ Python để chạy được test ở máy.
* [ADR-012](012-cach-ly-tenant-va-so-huu-vector.md): **`retrieval` sở hữu schema `vector`**
  — nó có quyền đọc-ghi trực tiếp ở đó, vì lọc hiệu lực phải nằm cùng một câu SQL với
  vector search.

Nhưng `retrieval` là service **Python**. Nếu nó sở hữu schema mà lược đồ lại do EF Core sinh
ra, thì ai chạy `dotnet ef migrations add` cho những bảng ấy? Câu hỏi này chưa được trả lời
ở đâu, và nó sẽ được trả lời **ngầm** bởi người đầu tiên cần một bảng vector — bằng bất cứ
cách nào tiện nhất lúc đó.

## Quyết định

**Tách quyền sở hữu *dữ liệu* khỏi quyền tác giả *DDL*.**

| | Ai |
|---|---|
| Quyền đọc-ghi **dữ liệu** trong `vector` | `retrieval` (giữ nguyên ADR-012) |
| Quyền tác giả **DDL** của `vector` | EF Core, qua một `DbContext` trong `Xnk.Corpus` (giữ nguyên ADR-010) |
| Hợp đồng mà `retrieval` đọc | `db/migrations/*.sql` — không đổi |

Nói cách khác: mô hình bảng vector khai bằng C#, migration xuất ra `.sql`, còn mọi câu
`SELECT`/`INSERT` trên dữ liệu đó do `retrieval` viết bằng SQL của chính nó. Vai trò
`xnk_retrieval` trong `db/roles.sql` được cấp `crud` trên `vector` và **không** có quyền
DDL — đúng như mọi service khác trên schema của mình.

**Schema `vector` chưa được tạo trong đợt dựng skeleton này.** Nó thuộc epic `E3`
(truy xuất), và tạo sẵn một schema rỗng chỉ để "có đủ bộ" là thêm một thứ phải bảo trì mà
chưa ai dùng. Vì vậy readiness của `retrieval` hiện chỉ kiểm schema `corpus`; dòng
`('xnk_retrieval', 'vector', 'crud')` trong `db/roles.sql` sẽ tự có hiệu lực khi schema
xuất hiện, nhờ vòng lặp ở đó bỏ qua schema chưa tồn tại.

## Hệ quả

- Đổi lược đồ `vector` là một PR chạm `src/dotnet/` **và** `db/migrations/` — tức phải qua
  CODEOWNERS của `data-lead` + `backend-lead` và ≥2 approval. Đó là **chủ đích**: hai service
  cùng chạm một cụm bảng, nên thay đổi lược đồ ở đó cần nhiều mắt hơn bình thường.
- Đội Python không tự thêm bảng được. Cái giá phải trả để giữ đúng một nguồn sinh lược đồ
  và đúng một định dạng hợp đồng.
- Khi `E3` bắt đầu, việc đầu tiên là một migration EF tạo schema `vector` cộng chỉ mục
  pgvector — extension `vector` đã được tạo sẵn trong migration của `corpus` vì nó thuộc
  phạm vi toàn database.

## Phương án đã cân nhắc và loại bỏ

**Alembic cho riêng `vector`.** Gần `retrieval` và pgvector nhất, và đội Python tự chủ được.
Loại bỏ vì nó tạo ra **hai** nguồn sinh lược đồ và hai định dạng migration trong cùng một
database — rồi câu hỏi "chạy cái nào trước" xuất hiện ở mọi môi trường. ADR-010 đã loại
Alembic vì lý do gần như vậy.

**`retrieval` tạo bảng lúc khởi động nếu chưa có.** Rất tiện ở local. Loại bỏ vì nhiều
replica khởi động cùng lúc sẽ tranh nhau, và nó đòi service phải có quyền DDL lúc chạy —
đúng thứ mà `db/roles.sql` cố ý không cấp cho bất kỳ service nào.

**Chuyển quyền sở hữu `vector` sang `corpus-service`.** Ranh giới sạch hơn về mặt sơ đồ.
Loại bỏ vì ADR-012 đã cân nhắc và bác: nó thêm một chặng mạng cho thao tác sinh embedding
hàng loạt, và buộc `corpus-service` phải biết về chiều vector cùng mô hình embedding —
kiến thức thuộc về `retrieval`.
