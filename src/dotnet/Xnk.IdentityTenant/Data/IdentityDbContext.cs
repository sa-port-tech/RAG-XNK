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
            entity.ToTable("users", t => t.HasCheckConstraint(
                "CK_users_Role",
                $"\"Role\" IN ({string.Join(", ", VaiTroHopLe.Select(v => $"'{v}'"))})"));

            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).HasMaxLength(320).IsRequired();
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
