using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xnk.TestSupport;
using Xnk.Corpus.Data;
using Xnk.Shared.Tenancy;

namespace Xnk.Corpus.Tests.TenantIsolation;

/// <summary>
/// Một PostgreSQL thật có pgvector, dùng chung cho cả nhóm test cách ly tenant.
/// </summary>
/// <remarks>
/// <para>
/// Vì sao là database thật chứ không phải provider in-memory: bộ lọc cách ly tenant nằm
/// trong mệnh đề WHERE do EF sinh ra và chạy ở tầng SQL (docs/00 §12). Provider in-memory
/// bỏ qua phần lớn ngữ nghĩa SQL, nên một test xanh trên in-memory không chứng minh được
/// điều mà test này sinh ra để chứng minh.
/// </para>
/// <para>
/// Hai chế độ, chọn tự động:
/// </para>
/// <list type="bullet">
/// <item>
/// Có biến <c>XNK_TEST_DATABASE_URL</c> → dùng luôn. Đây là đường đi trên CI: job
/// <c>tenant-isolation</c> đã dựng sẵn một service container <c>pgvector/pgvector:pg16</c>
/// và truyền chuỗi kết nối qua biến này.
/// </item>
/// <item>
/// Không có → tự khởi một container cùng ảnh. Đây là đường đi trên máy dev: chạy
/// <c>dotnet test</c> ngay sau khi clone, không cần dựng gì trước.
/// </item>
/// </list>
/// <para>
/// Dùng đúng một ảnh cho cả hai đường là có chủ đích — khác phiên bản PostgreSQL giữa máy
/// dev và CI là cách tạo ra loại lỗi "chỉ đỏ trên CI" mà không ai tái hiện được.
/// </para>
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime
{
    /// <summary>Biến môi trường mà CI dùng để trỏ tới PostgreSQL có sẵn.</summary>
    /// <remarks>Dùng chung với ba service Python — xem <see cref="ChuoiKetNoiTest"/>.</remarks>
    public const string ConnectionEnvironmentVariable = ChuoiKetNoiTest.TenBienMoiTruong;

    /// <summary>Ảnh container, khớp đúng ảnh trong <c>.github/workflows/ci-dotnet.yml</c>.</summary>
    public const string PostgresImage = ChuoiKetNoiTest.AnhPostgres;

    private PostgreSqlContainer? _container;

    /// <summary>Chuỗi kết nối tới database dùng cho test.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        string? provided = ChuoiKetNoiTest.TuMoiTruong();

        if (provided is null)
        {
            // Ảnh truyền qua constructor: Testcontainers 4.14 đã bỏ constructor không
            // tham số kèm WithImage.
            _container = new PostgreSqlBuilder(PostgresImage)
                .WithDatabase("xnk_test")
                .Build();

            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
        }
        else
        {
            ConnectionString = provided;
        }

        // Áp migration thật thay vì EnsureCreated(): EnsureCreated dựng lược đồ từ model
        // và bỏ qua hoàn toàn thư mục migration, nên nó sẽ báo xanh kể cả khi migration
        // hỏng. Chạy migration ở đây biến chính bộ test này thành phép kiểm chứng rằng
        // db/migrations áp được lên một database trống.
        await using CorpusDbContext context = CreateContext(tenantId: null);
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Dựng một <see cref="CorpusDbContext"/> đóng vai một tenant cụ thể.
    /// </summary>
    /// <param name="tenantId">Tenant cần giả lập, <c>null</c> nghĩa là không có ngữ cảnh tenant.</param>
    public CorpusDbContext CreateContext(Guid? tenantId)
    {
        DbContextOptions<CorpusDbContext> options =
            new DbContextOptionsBuilder<CorpusDbContext>()
                .UseNpgsql(ConnectionString, npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history", CorpusDbContext.SchemaName))
                .Options;

        return new CorpusDbContext(options, new StaticTenantContext(tenantId));
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

/// <summary>
/// Gom các test cách ly tenant vào một collection để container chỉ khởi động một lần.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    /// <summary>Tên collection.</summary>
    public const string Name = "postgres";
}
