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
        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TenantId", sql, StringComparison.Ordinal);
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
