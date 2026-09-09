using Microsoft.EntityFrameworkCore;

namespace Xnk.IdentityTenant.Data;

/// <summary>
/// Ngữ cảnh dữ liệu của identity-tenant. Sở hữu schema <c>identity</c> (docs/00 §4.2).
/// </summary>
/// <remarks>
/// ⚠️ Context này **cố ý không có global query filter theo tenant**, khác với
/// <c>CorpusDbContext</c>.
/// <para>
/// Lý do là thứ tự nhân quả: bộ lọc tenant ở nơi khác đọc <c>tenant_id</c> từ token, mà
/// token thì do chính service này phát ra. Lúc tra bảng <c>users</c> để kiểm mật khẩu,
/// chưa có token nào tồn tại — lọc theo tenant ở đây sẽ luôn cho ra tập rỗng và không ai
/// đăng nhập được.
/// </para>
/// <para>
/// Đổi lại, mọi truy vấn trong service này phải tự giới hạn phạm vi một cách tường minh.
/// Đó là lý do lớp này nằm sau một API rất hẹp: một endpoint phát token, không có endpoint
/// nào liệt kê người dùng. Khi thêm API quản trị (RBAC, quota), phạm vi tenant phải được
/// kiểm ngay trong handler và có test đi kèm — đừng trông chờ tầng dữ liệu chặn hộ.
/// </para>
/// </remarks>
/// <param name="options">Tuỳ chọn do DI cung cấp.</param>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    /// <summary>Tên schema mà service này sở hữu.</summary>
    public const string SchemaName = "identity";

    /// <summary>Ba loại tenant hợp lệ (docs/00 §12).</summary>
    /// <remarks>
    /// Khớp phần mô tả ở <see cref="Xnk.Shared.Tenancy.TenantClaims.TenantType"/>. Được
    /// dùng để sinh ràng buộc CHECK, nên thêm loại mới là một migration, không phải một
    /// dòng cấu hình.
    /// </remarks>
    public static readonly string[] LoaiTenantHopLe = ["noi_bo", "b2b_khach_hang", "dao_tao"];

    /// <summary>Ba vai trò hợp lệ.</summary>
    public static readonly string[] VaiTroHopLe = ["admin", "user", "viewer"];

    /// <summary>Các tổ chức.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Người dùng.</summary>
    public DbSet<User> Users => Set<User>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants", t => t.HasCheckConstraint(
                "CK_tenants_Type",
                $"\"Type\" IN ({string.Join(", ", LoaiTenantHopLe.Select(v => $"'{v}'"))})"));

            entity.HasKey(t => t.Id);
            entity.Property(t => t.Slug).HasMaxLength(64).IsRequired();
            entity.Property(t => t.Name).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Type).HasMaxLength(32).IsRequired();
            entity.Property(t => t.CreatedAt).HasDefaultValueSql("now()");

            entity.HasIndex(t => t.Slug).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users", t =>
            {
                t.HasCheckConstraint(
                    "CK_users_Role",
                    $"\"Role\" IN ({string.Join(", ", VaiTroHopLe.Select(v => $"'{v}'"))})");

                // Email phải đã là chữ thường TRƯỚC KHI chạm bảng.
                //
                // Bộ chuyển đổi ngay bên dưới lo phần đi qua EF, nhưng không phải mọi thứ
                // ghi vào bảng này đều đi qua EF: db/seed/0003 là SQL thuần, và một lần
                // import hay một lần sửa tay cũng vậy. Ràng buộc này là chỗ duy nhất áp
                // được cho tất cả.
                //
                // Vì sao nó quan trọng: chỉ mục duy nhất trên "Email" PHÂN BIỆT hoa
                // thường, nên nếu không có ràng buộc này thì `Admin@x.vn` và `admin@x.vn`
                // cùng tồn tại được — và bản viết hoa KHÔNG BAO GIỜ đăng nhập được, vì
                // TokenEndpoints hạ chữ thường trước khi tra. Triệu chứng là "mật khẩu
                // sai", tức là chỗ cuối cùng người ta nghĩ tới sẽ là kiểu chữ của email.
                //
                // Có ràng buộc này rồi thì chỉ mục duy nhất thường trên "Email" tương
                // đương một chỉ mục không phân biệt hoa thường — không cần chỉ mục hàm.
                t.HasCheckConstraint("CK_users_Email_chu_thuong", "\"Email\" = lower(\"Email\")");
            });

            entity.HasKey(u => u.Id);
            // Hạ chữ thường khi GHI, và cả khi so sánh: bộ chuyển đổi áp cho tham số
            // truy vấn nữa, nên `u.Email == email` cũng tự hạ chữ thường vế phải. Nhờ đó
            // "chuẩn hoá email" thôi là một quy ước phải nhớ, thành một tính chất của mô
            // hình. Chiều đọc giữ nguyên chuỗi — dữ liệu trong bảng đã là chữ thường rồi.
            entity.Property(u => u.Email)
                .HasMaxLength(320)
                .IsRequired()
                .HasConversion(v => v.ToLowerInvariant(), v => v);
            entity.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(32).IsRequired();
            entity.Property(u => u.IsActive).HasDefaultValue(true);
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("now()");

            // Duy nhất toàn cục — xem chú thích ở User.Email.
            entity.HasIndex(u => u.Email).IsUnique();

            // Xoá tenant thì xoá người dùng của nó. Đây là quan hệ trong CÙNG một schema
            // nên khoá ngoại hoàn toàn hợp lệ; ràng buộc bị cấm là ràng buộc CHÉO schema.
            entity.HasOne(u => u.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
