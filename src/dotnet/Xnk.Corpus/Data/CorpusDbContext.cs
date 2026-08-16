using Microsoft.EntityFrameworkCore;
using Xnk.Shared.Tenancy;

namespace Xnk.Corpus.Data;

/// <summary>
/// Ngữ cảnh dữ liệu của corpus-service. Sở hữu schema <c>corpus</c> (docs/00 §4.2).
/// </summary>
/// <param name="options">Tuỳ chọn do DI cung cấp.</param>
/// <param name="tenantContext">Tenant của request hiện tại.</param>
public sealed class CorpusDbContext(
    DbContextOptions<CorpusDbContext> options,
    ITenantContext tenantContext) : DbContext(options)
{
    /// <summary>Tên schema mà service này sở hữu.</summary>
    public const string SchemaName = "corpus";

    private readonly ITenantContext _tenantContext = tenantContext;

    /// <summary>
    /// Tenant dùng cho bộ lọc toàn cục.
    /// </summary>
    /// <remarks>
    /// Phải là thuộc tính CỦA CHÍNH context thì EF mới dịch nó thành tham số truy vấn và
    /// đọc lại giá trị ở mỗi lần chạy. Nếu bắt biến cục bộ trong biểu thức lọc, giá trị
    /// sẽ bị nướng cứng vào model đã cache — và mọi tenant sau đó dùng chung tenant của
    /// request đầu tiên. Đó chính là kiểu rò rỉ chéo mà bộ test cách ly tenant tồn tại
    /// để chặn.
    /// </remarks>
    public Guid? CurrentTenantId => _tenantContext.TenantId;

    /// <summary>Văn bản trong corpus.</summary>
    public DbSet<Document> Documents => Set<Document>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);

        // Extension pgvector là phạm vi TOÀN DATABASE, không thuộc riêng schema nào.
        // Khai ở đây vì migration của corpus hiện là migration duy nhất; các bảng vector
        // thật sự thuộc quyền của service retrieval (ADR-0004).
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(d => d.Id);

            entity.Property(d => d.DocumentNumber).HasMaxLength(100).IsRequired();
            entity.Property(d => d.Title).HasMaxLength(1000).IsRequired();
            entity.Property(d => d.CreatedAt).HasDefaultValueSql("now()");

            // Chỉ mục phục vụ chính bộ lọc bên dưới — mọi truy vấn đều đi qua nó.
            entity.HasIndex(d => d.TenantId);
            entity.HasIndex(d => new { d.EffectiveFrom, d.EffectiveTo });
            entity.HasIndex(d => d.DocumentNumber);

            // ⚠️ RANH GIỚI BẢO MẬT (docs/00 §12) ⚠️
            //
            // Bộ lọc này chạy ở tầng SQL, không phải tầng ứng dụng: EF chèn nó vào mệnh
            // đề WHERE của MỌI truy vấn trên Documents. Đó là yêu cầu tường minh của
            // §12 — lọc ở tầng ứng dụng nghĩa là chỉ cần một chỗ quên gọi hàm lọc là rò
            // rỉ, và chỗ quên đó không hiện ra trong bất kỳ diff nào.
            //
            // Ngữ nghĩa: thấy văn bản dùng chung (TenantId == null) cộng với văn bản của
            // chính tenant mình. Không có ngữ cảnh tenant thì chỉ thấy phần dùng chung.
            //
            // Bỏ dòng này hoặc gọi IgnoreQueryFilters() ở tầng nghiệp vụ là xoá lớp cách
            // ly. Nếu thật sự cần đọc xuyên tenant (ví dụ job quản trị), hãy làm bằng một
            // DbContext riêng có tên nói rõ điều đó, để nó hiện ra trong review.
            entity.HasQueryFilter(d => d.TenantId == null || d.TenantId == CurrentTenantId);
        });
    }
}
