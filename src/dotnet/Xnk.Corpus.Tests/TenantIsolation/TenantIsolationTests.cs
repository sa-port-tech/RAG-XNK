using Microsoft.EntityFrameworkCore;
using Xnk.Corpus.Data;

namespace Xnk.Corpus.Tests.TenantIsolation;

/// <summary>
/// Chứng minh dữ liệu riêng của tenant này không lọt sang tenant khác.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ Đây là cổng bảo mật BR-11, một trong ba required check ở docs/16 §3. Job
/// <c>tenant-isolation</c> trong ci-dotnet chạy riêng nhóm test này với
/// <c>TreatNoTestsAsError</c> bật — xoá hết trait <c>TenantIsolation</c> là làm đỏ CI,
/// có chủ đích.
/// </para>
/// <para>
/// Mỗi test dùng GUID tenant sinh mới nên chạy song song và chạy lại trên cùng một
/// database dùng chung (trường hợp CI) đều không giẫm lên nhau.
/// </para>
/// </remarks>
[Collection(PostgresCollection.Name)]
[Trait("Category", "TenantIsolation")]
public sealed class TenantIsolationTests(PostgresFixture postgres)
{
    private readonly PostgresFixture _postgres = postgres;

    [Fact]
    public async Task Tenant_chi_thay_van_ban_dung_chung_va_cua_chinh_minh()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        string moc = Guid.NewGuid().ToString("N");

        await SeedAsync(moc, tenantA, tenantB);

        await using CorpusDbContext contextA = _postgres.CreateContext(tenantA);
        List<Document> nhinThayBoiA = await contextA.Documents
            .Where(d => d.Title.EndsWith(moc))
            .ToListAsync();

        Assert.Equal(2, nhinThayBoiA.Count);
        Assert.Contains(nhinThayBoiA, d => d.TenantId == null);
        Assert.Contains(nhinThayBoiA, d => d.TenantId == tenantA);
        Assert.DoesNotContain(nhinThayBoiA, d => d.TenantId == tenantB);
    }

    [Fact]
    public async Task Hai_tenant_khong_thay_du_lieu_cua_nhau()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        string moc = Guid.NewGuid().ToString("N");

        await SeedAsync(moc, tenantA, tenantB);

        await using CorpusDbContext contextB = _postgres.CreateContext(tenantB);
        List<Document> nhinThayBoiB = await contextB.Documents
            .Where(d => d.Title.EndsWith(moc))
            .ToListAsync();

        Assert.Contains(nhinThayBoiB, d => d.TenantId == tenantB);
        Assert.DoesNotContain(nhinThayBoiB, d => d.TenantId == tenantA);
    }

    [Fact]
    public async Task Khong_co_ngu_canh_tenant_thi_chi_thay_phan_dung_chung()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        string moc = Guid.NewGuid().ToString("N");

        await SeedAsync(moc, tenantA, tenantB);

        // Ngữ cảnh null xuất hiện ở job nền và công cụ dòng lệnh. Nó phải nghiêng về
        // phía thấy ÍT hơn, không phải thấy tất cả — một lỗi quên gắn tenant khi đó biến
        // thành mất dữ liệu nhìn thấy ngay, chứ không thành rò rỉ im lặng.
        await using CorpusDbContext context = _postgres.CreateContext(tenantId: null);
        List<Document> nhinThay = await context.Documents
            .Where(d => d.Title.EndsWith(moc))
            .ToListAsync();

        Assert.Single(nhinThay);
        Assert.Null(nhinThay[0].TenantId);
    }

    [Fact]
    public void Bo_loc_tenant_nam_trong_cau_SQL_chu_khong_o_tang_ung_dung()
    {
        Guid tenantA = Guid.NewGuid();

        using CorpusDbContext context = _postgres.CreateContext(tenantA);
        string sql = context.Documents.ToQueryString();

        // docs/00 §12 yêu cầu tường minh: lọc trong mệnh đề WHERE của chính truy vấn ở
        // tầng database, KHÔNG lọc ở tầng application. Ba test trên vẫn xanh nếu ai đó
        // chuyển việc lọc lên C# — test này thì không.
        //
        // ⚠️ Bản trước của test này là `Assert.Contains("TenantId", sql)` trên TOÀN BỘ câu
        // SQL. Phép kiểm đó **luôn đúng**: `TenantId` nằm sẵn trong danh sách SELECT của
        // mọi truy vấn trên bảng này. Gỡ hẳn `HasQueryFilter` đi thì nó vẫn xanh — tức là
        // cổng BR-11, thứ mà docs/16 dùng để nâng độ phủ lên 1/11, không kiểm gì cả.
        int viTriWhere = sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
        Assert.True(viTriWhere >= 0, $"Truy vấn không có mệnh đề WHERE nào. SQL: {sql}");

        // Chỉ soi phần SAU `WHERE`, để lần xuất hiện trong danh sách SELECT không tính.
        string menhDeWhere = sql[viTriWhere..];
        Assert.Contains("TenantId", menhDeWhere, StringComparison.Ordinal);
    }

    [Fact]
    public void Go_bo_loc_toan_cuc_di_thi_cau_SQL_PHAI_khac()
    {
        // Phép kiểm không phụ thuộc vào cách EF đặt tên tham số hay xuống dòng: so câu SQL
        // có bộ lọc với chính nó khi bộ lọc bị tắt. Hai câu giống nhau nghĩa là bộ lọc
        // không đóng góp gì vào SQL — dù `HasQueryFilter` còn nằm đó hay không.
        //
        // Đây là vế mà một assertion dạng "chuỗi có chứa X" không bao giờ bắt được.
        Guid tenantA = Guid.NewGuid();

        using CorpusDbContext context = _postgres.CreateContext(tenantA);

        string coLoc = context.Documents.ToQueryString();
        string khongLoc = context.Documents.IgnoreQueryFilters().ToQueryString();

        Assert.NotEqual(khongLoc, coLoc);
        Assert.DoesNotContain("WHERE", khongLoc, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Tenant_nay_KHONG_xoa_duoc_ban_ghi_cua_tenant_kia()
    {
        // Bộ lọc toàn cục của EF áp cho truy vấn ĐỌC. Câu hỏi hiển nhiên tiếp theo — "thế
        // còn ghi?" — chưa test nào trả lời, và ranh giới cách ly chỉ có một vế thì nó là
        // nửa ranh giới.
        //
        // ExecuteDelete/ExecuteUpdate dịch thẳng thành DELETE/UPDATE ... WHERE, và EF áp
        // bộ lọc vào phần WHERE đó. Test này khoá lại điều ấy: nếu một phiên bản EF sau
        // đổi hành vi, hoặc ai đó thêm IgnoreQueryFilters cho "tiện", nó đỏ.
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        string moc = Guid.NewGuid().ToString("N");

        await SeedAsync(moc, tenantA, tenantB);

        await using CorpusDbContext cuaA = _postgres.CreateContext(tenantA);
        int soDongXoa = await cuaA.Documents
            .Where(d => d.Title.EndsWith(moc) && d.TenantId == tenantB)
            .ExecuteDeleteAsync();

        Assert.Equal(0, soDongXoa);

        // Và bản ghi của B vẫn còn nguyên khi nhìn bằng ngữ cảnh của B.
        await using CorpusDbContext cuaB = _postgres.CreateContext(tenantB);
        Assert.Single(await cuaB.Documents
            .Where(d => d.Title.EndsWith(moc) && d.TenantId == tenantB)
            .ToListAsync());
    }

    /// <summary>
    /// Nạp ba văn bản mang cùng một mốc: một dùng chung, một của A, một của B.
    /// </summary>
    /// <remarks>
    /// Nạp bằng ngữ cảnh <c>null</c>: bộ lọc toàn cục chỉ áp cho truy vấn đọc, không áp
    /// cho thao tác ghi, nên vẫn đặt được <c>TenantId</c> tuỳ ý. Chính điều đó làm cho
    /// bài test có nghĩa — dữ liệu của B thật sự nằm trong bảng, và việc A không thấy nó
    /// là do bộ lọc chứ không phải do dữ liệu chưa từng tồn tại.
    /// </remarks>
    private async Task SeedAsync(string moc, Guid tenantA, Guid tenantB)
    {
        await using CorpusDbContext seeder = _postgres.CreateContext(tenantId: null);

        seeder.Documents.AddRange(
            new Document
            {
                Id = Guid.NewGuid(),
                TenantId = null,
                DocumentNumber = "39/2018/TT-BTC",
                Title = $"Thông tư dùng chung {moc}",
                EffectiveFrom = new DateOnly(2018, 6, 5),
            },
            new Document
            {
                Id = Guid.NewGuid(),
                TenantId = tenantA,
                DocumentNumber = "SOP-A-01",
                Title = $"SOP nội bộ của tenant A {moc}",
            },
            new Document
            {
                Id = Guid.NewGuid(),
                TenantId = tenantB,
                DocumentNumber = "SOP-B-01",
                Title = $"SOP nội bộ của tenant B {moc}",
            });

        await seeder.SaveChangesAsync();
    }
}
