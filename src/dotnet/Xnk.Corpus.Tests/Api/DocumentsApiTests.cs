using System.Net;
using System.Net.Http.Json;
using Xnk.Corpus.Contracts;
using Xnk.Corpus.Data;
using Xnk.Corpus.Tests.TenantIsolation;

namespace Xnk.Corpus.Tests.Api;

/// <summary>
/// API văn bản, kiểm qua HTTP với token của hai tenant khác nhau.
/// </summary>
/// <remarks>
/// Bộ test trong <c>TenantIsolation</c> chứng minh bộ lọc chạy ở tầng SQL. Bộ này chứng
/// minh nó **còn nguyên tác dụng sau khi đi qua cả chồng HTTP** — token → middleware xác
/// thực → <c>HttpTenantContext</c> → global query filter. Hai chuyện khác nhau: lớp thứ
/// hai từng hỏng vì những lý do chẳng liên quan gì tới SQL, ví dụ quên
/// <c>UseAuthentication</c> hay đặt <c>[Authorize]</c> sai chỗ.
/// </remarks>
[Collection(PostgresCollection.Name)]
public sealed class DocumentsApiTests(PostgresFixture postgres) : IAsyncLifetime
{
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();
    private readonly string _soHieuChung = $"CHUNG-{Guid.NewGuid():N}";
    private readonly string _soHieuA = $"A-{Guid.NewGuid():N}";
    private readonly string _soHieuB = $"B-{Guid.NewGuid():N}";

    private CorpusApiFactory _factory = null!;
    private Guid _idVanBanCuaA;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _factory = new CorpusApiFactory(postgres.ConnectionString);

        // Ghi dữ liệu bằng context KHÔNG có ngữ cảnh tenant. Bộ lọc chỉ áp cho việc ĐỌC,
        // nên đây là cách duy nhất dựng được dữ liệu của nhiều tenant trong một lần.
        await using CorpusDbContext db = postgres.CreateContext(tenantId: null);

        Document vanBanCuaA = TaoVanBan(_soHieuA, _tenantA);
        db.Documents.AddRange(
            TaoVanBan(_soHieuChung, tenantId: null),
            vanBanCuaA,
            TaoVanBan(_soHieuB, _tenantB));

        await db.SaveChangesAsync();
        _idVanBanCuaA = vanBanCuaA.Id;
    }

    /// <inheritdoc />
    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Khong_co_token_thi_bi_tu_choi()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage phanHoi = await client.GetAsync(new Uri("/corpus/documents", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, phanHoi.StatusCode);
    }

    [Fact]
    public async Task Duong_dan_co_tien_to_la_duong_duoc_phuc_vu()
    {
        // ADR-013: ALB không cắt tiền tố, nên ứng dụng nhận nguyên `/corpus/...`.
        using HttpClient client = _factory.TaoClientCuaTenant(_tenantA);

        HttpResponseMessage coTienTo = await client.GetAsync(new Uri("/corpus/documents", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, coTienTo.StatusCode);

        // ⚠️ Khác biệt cần biết giữa hai stack, ghi ra để không ai coi là lỗi:
        //
        // Service .NET dùng `UsePathBase("/corpus")`, và middleware đó chỉ CẮT tiền tố khi
        // nó có mặt — route vẫn khai là `/documents`. Hệ quả: đường dẫn KHÔNG tiền tố cũng
        // chạy. Ba service Python thì gắn prefix thẳng vào router, nên ở đó đường trần trả
        // 404 (xem test_khong_co_tien_to_thi_404 bên retrieval).
        //
        // Cả hai đều đúng sau ALB, vì ALB chỉ chuyển tiếp đường CÓ tiền tố. Test này khoá
        // lại hành vi thật của .NET thay vì khoá một kỳ vọng chép nhầm từ phía Python.
        HttpResponseMessage khongTienTo = await client.GetAsync(new Uri("/documents", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, khongTienTo.StatusCode);
    }

    [Fact]
    public async Task Moi_tenant_chi_thay_van_ban_dung_chung_va_cua_chinh_minh()
    {
        using HttpClient clientA = _factory.TaoClientCuaTenant(_tenantA);
        using HttpClient clientB = _factory.TaoClientCuaTenant(_tenantB);

        List<string> cuaA = await LaySoHieu(clientA);
        List<string> cuaB = await LaySoHieu(clientB);

        Assert.Contains(_soHieuChung, cuaA);
        Assert.Contains(_soHieuA, cuaA);
        Assert.DoesNotContain(_soHieuB, cuaA);

        Assert.Contains(_soHieuChung, cuaB);
        Assert.Contains(_soHieuB, cuaB);
        Assert.DoesNotContain(_soHieuA, cuaB);
    }

    [Fact]
    public async Task Doc_van_ban_cua_tenant_khac_thi_404_chu_khong_phai_403()
    {
        // 403 xác nhận bản ghi đó tồn tại, và một người kiên nhẫn dò được danh sách khoá
        // chính hợp lệ của tenant khác chỉ bằng cách đọc mã trạng thái.
        using HttpClient clientB = _factory.TaoClientCuaTenant(_tenantB);

        HttpResponseMessage phanHoi = await clientB.GetAsync(
            new Uri($"/corpus/documents/{_idVanBanCuaA}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, phanHoi.StatusCode);
    }

    [Fact]
    public async Task Chu_so_huu_doc_duoc_van_ban_cua_minh()
    {
        using HttpClient clientA = _factory.TaoClientCuaTenant(_tenantA);

        DocumentSummary? vanBan = await clientA.GetFromJsonAsync<DocumentSummary>(
            new Uri($"/corpus/documents/{_idVanBanCuaA}", UriKind.Relative));

        Assert.NotNull(vanBan);
        Assert.Equal(_soHieuA, vanBan.DocumentNumber);
        Assert.False(vanBan.IsShared);
    }

    [Fact]
    public async Task Tong_so_dem_sau_khi_loc_chu_khong_phai_tong_ca_bang()
    {
        // Trả về tổng thật của bảng sẽ rò rỉ quy mô dữ liệu của tenant khác — không lộ nội
        // dung nào, nhưng vẫn là rò rỉ.
        using HttpClient clientA = _factory.TaoClientCuaTenant(_tenantA);

        PagedResult<DocumentSummary>? trang = await clientA.GetFromJsonAsync<PagedResult<DocumentSummary>>(
            new Uri("/corpus/documents?pageSize=100", UriKind.Relative));

        Assert.NotNull(trang);
        Assert.Equal(trang.Items.Count, trang.TotalCount);
        Assert.DoesNotContain(trang.Items, d => d.DocumentNumber == _soHieuB);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    public async Task Tham_so_phan_trang_sai_thi_400(string chuoiTruyVan)
    {
        using HttpClient clientA = _factory.TaoClientCuaTenant(_tenantA);

        HttpResponseMessage phanHoi = await clientA.GetAsync(
            new Uri($"/corpus/documents{chuoiTruyVan}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, phanHoi.StatusCode);
    }

    private static async Task<List<string>> LaySoHieu(HttpClient client)
    {
        PagedResult<DocumentSummary>? trang = await client.GetFromJsonAsync<PagedResult<DocumentSummary>>(
            new Uri("/corpus/documents?pageSize=100", UriKind.Relative));

        Assert.NotNull(trang);
        return [.. trang.Items.Select(d => d.DocumentNumber)];
    }

    private static Document TaoVanBan(string soHieu, Guid? tenantId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        DocumentNumber = soHieu,
        Title = $"Văn bản dùng cho test {soHieu}",
    };
}
