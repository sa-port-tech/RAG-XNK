using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Xnk.IdentityTenant.Data;

/// <summary>
/// Dựng <see cref="IdentityDbContext"/> cho công cụ dòng lệnh <c>dotnet ef</c>.
/// </summary>
/// <remarks>
/// Cùng lý do như <c>DesignTimeCorpusDbContextFactory</c>: không có lớp này thì
/// <c>dotnet ef</c> phải khởi động cả ứng dụng — kéo theo yêu cầu có khoá JWT hợp lệ và
/// một database đang chạy — chỉ để sinh một file migration.
/// </remarks>
public sealed class DesignTimeIdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    /// <summary>Biến môi trường chứa chuỗi kết nối dùng lúc thiết kế.</summary>
    public const string ConnectionEnvironmentVariable = "XNK_IDENTITY_CONNECTION";

    /// <inheritdoc />
    public IdentityDbContext CreateDbContext(string[] args)
    {
        // Chuỗi dự phòng chỉ để EF biết đang nói chuyện với PostgreSQL — sinh migration
        // không kết nối tới database.
        string connection = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable)
            ?? "Host=localhost;Port=5432;Database=xnk;Username=postgres;Password=postgres";

        DbContextOptions<IdentityDbContext> options =
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseNpgsql(connection, npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history", IdentityDbContext.SchemaName))
                .Options;

        return new IdentityDbContext(options);
    }
}
