using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Xnk.Shared.Tenancy;

namespace Xnk.Corpus.Data;

/// <summary>
/// Dựng <see cref="CorpusDbContext"/> cho công cụ dòng lệnh <c>dotnet ef</c>.
/// </summary>
/// <remarks>
/// <para>
/// Không có lớp này thì <c>dotnet ef</c> phải khởi động toàn bộ ứng dụng để lấy được
/// context — kéo theo việc phải có khoá JWT hợp lệ và một database đang chạy chỉ để sinh
/// một file migration. Đó là cách biến một thao tác ngoại tuyến thành một thao tác cần
/// hạ tầng.
/// </para>
/// <para>
/// Ngữ cảnh tenant ở đây cố định <c>null</c>: sinh migration là thao tác về lược đồ, hoàn
/// toàn không liên quan tới dữ liệu của tenant nào.
/// </para>
/// </remarks>
public sealed class DesignTimeCorpusDbContextFactory : IDesignTimeDbContextFactory<CorpusDbContext>
{
    /// <summary>Biến môi trường chứa chuỗi kết nối dùng lúc thiết kế.</summary>
    public const string ConnectionEnvironmentVariable = "XNK_CORPUS_CONNECTION";

    /// <inheritdoc />
    public CorpusDbContext CreateDbContext(string[] args)
    {
        // Chuỗi dự phòng chỉ để EF biết đang nói chuyện với PostgreSQL — sinh migration
        // không kết nối tới database. `dotnet ef database update` thì cần chuỗi thật, và
        // khi đó phải đặt biến môi trường.
        string connection = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable)
            ?? "Host=localhost;Port=5432;Database=xnk;Username=postgres;Password=postgres";

        DbContextOptions<CorpusDbContext> options =
            new DbContextOptionsBuilder<CorpusDbContext>()
                .UseNpgsql(connection, npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history", CorpusDbContext.SchemaName))
                .Options;

        return new CorpusDbContext(options, new StaticTenantContext(null));
    }
}
