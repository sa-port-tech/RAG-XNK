using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xnk.TestSupport;
using Xnk.IdentityTenant.Data;

namespace Xnk.IdentityTenant.Tests.Api;

/// <summary>
/// Một PostgreSQL thật cho nhóm test của identity-tenant.
/// </summary>
/// <remarks>
/// Cùng khuôn với <c>PostgresFixture</c> của Xnk.Corpus.Tests, và cùng lý do: hai chế độ
/// chọn tự động — có <c>XNK_TEST_DATABASE_URL</c> thì dùng luôn (đường đi trên CI và trong
/// container test), không có thì tự khởi container cùng ảnh mà CI dùng (đường đi trên máy
/// dev, chạy được ngay sau khi clone).
/// <para>
/// Không gộp hai fixture vào một thư viện chung: hai service sở hữu hai lược đồ khác nhau
/// và migration của chúng chạy độc lập. Gộp lại là tạo một phụ thuộc giữa hai bộ test mà
/// bản thân hai service không có.
/// </para>
/// </remarks>
public sealed class IdentityPostgresFixture : IAsyncLifetime
{
    /// <summary>Biến môi trường mà CI dùng để trỏ tới PostgreSQL có sẵn.</summary>
    public const string ConnectionEnvironmentVariable = ChuoiKetNoiTest.TenBienMoiTruong;

    /// <summary>Ảnh container, khớp đúng ảnh trong ci-dotnet.yml và docker-compose.yml.</summary>
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

        // Áp migration thật thay vì EnsureCreated(): EnsureCreated dựng lược đồ từ model và
        // bỏ qua hoàn toàn thư mục migration, nên nó báo xanh kể cả khi migration hỏng.
        await using IdentityDbContext db = CreateContext();
        await db.Database.MigrateAsync();
    }

    /// <summary>Dựng một <see cref="IdentityDbContext"/> trỏ tới database test.</summary>
    public IdentityDbContext CreateContext()
    {
        DbContextOptions<IdentityDbContext> options =
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseNpgsql(ConnectionString, npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history", IdentityDbContext.SchemaName))
                .Options;

        return new IdentityDbContext(options);
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

/// <summary>Gom test của identity vào một collection để container chỉ khởi động một lần.</summary>
[CollectionDefinition(Name)]
public sealed class IdentityPostgresCollection : ICollectionFixture<IdentityPostgresFixture>
{
    /// <summary>Tên collection.</summary>
    public const string Name = "identity-postgres";
}
