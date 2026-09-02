using System.Net;
using System.Net.Http.Json;
using Xnk.Chat.Contracts;

namespace Xnk.Chat.Tests;

/// <summary>
/// Endpoint hỏi–đáp, gọi qua HTTP thật với hai service phía sau được thay bằng handler ghi lại.
/// </summary>
public sealed class ChatApiTests : IAsyncLifetime
{
    private const string _trangRetrieval = """
        {"items":[{"id":"1","document_number":"39/2018/TT-BTC","title":"Sửa đổi TT 38/2015","is_shared":true},
                  {"id":"2","document_number":"SOP-NB-01","title":"Quy trình nội bộ","is_shared":false}],
         "total_count":2}
        """;

    private const string _phanHoiGeneration = """
        {"answer":"Theo Thông tư 39/2018/TT-BTC…","model":"mo-hinh-dung-cho-test"}
        """;

    private readonly Guid _tenant = Guid.NewGuid();
    private ChatApiFactory _factory = null!;

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        _factory = new ChatApiFactory();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Khong_co_token_thi_bi_tu_choi()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage phanHoi = await client.PostAsJsonAsync(
            new Uri("/chat/ask", UriKind.Relative), new { question = "Hỏi gì đó?" });

        Assert.Equal(HttpStatusCode.Unauthorized, phanHoi.StatusCode);
        // Không token thì KHÔNG được chạm tới service nào phía sau.
        Assert.Empty(_factory.Retrieval.DaGoi);
        Assert.Empty(_factory.Generation.DaGoi);
    }

    [Fact]
    public async Task Tra_ve_cau_tra_loi_kem_can_cu()
    {
        _factory.Retrieval.TraVe(HttpStatusCode.OK, _trangRetrieval);
        _factory.Generation.TraVe(HttpStatusCode.OK, _phanHoiGeneration);

        using HttpClient client = _factory.TaoClientCuaTenant(_tenant);

        ChatResponse? ketQua = await client.GetChatResponse("Thủ tục hải quan?");

        Assert.NotNull(ketQua);
        Assert.Equal("Theo Thông tư 39/2018/TT-BTC…", ketQua.Answer);
        Assert.Equal("mo-hinh-dung-cho-test", ketQua.Model);
        Assert.Equal(2, ketQua.Citations.Count);
        Assert.Contains(ketQua.Citations, c => c.DocumentNumber == "SOP-NB-01" && !c.IsShared);
    }

    [Fact]
    public async Task Token_cua_nguoi_dung_duoc_chuyen_tiep_xuong_CA_HAI_service()
    {
        // ⚠️ Đây là test quan trọng nhất của bộ này.
        //
        // Mất mắt xích chuyển tiếp thì `retrieval` lọc dữ liệu dưới một danh tính khác —
        // hoặc không danh tính nào — và lớp cách ly tenant mất tác dụng ở chặng thứ hai dù
        // mã của nó vẫn đúng từng chữ. Lỗ hổng đó không hiện ra trong diff hay test của
        // `retrieval`, vì phía đó mọi thứ vẫn hoạt động y như thiết kế.
        _factory.Retrieval.TraVe(HttpStatusCode.OK, _trangRetrieval);
        _factory.Generation.TraVe(HttpStatusCode.OK, _phanHoiGeneration);

        string token = _factory.TaoToken(_tenant);
        using HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        await client.GetChatResponse("Hỏi gì đó?");

        HttpRequestMessage goiRetrieval = Assert.Single(_factory.Retrieval.DaGoi);
        HttpRequestMessage goiGeneration = Assert.Single(_factory.Generation.DaGoi);

        Assert.Equal(token, goiRetrieval.Headers.Authorization?.Parameter);
        Assert.Equal(token, goiGeneration.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task Khong_gui_tham_so_tenant_nao_xuong_retrieval()
    {
        // Tenant đến từ token, và chỉ từ token. Gửi kèm một tham số tenant là tạo ra hai
        // nguồn sự thật cho cùng một điều — nguồn dễ giả mạo hơn nằm ở phía người gọi.
        _factory.Retrieval.TraVe(HttpStatusCode.OK, _trangRetrieval);
        _factory.Generation.TraVe(HttpStatusCode.OK, _phanHoiGeneration);

        using HttpClient client = _factory.TaoClientCuaTenant(_tenant);
        await client.GetChatResponse("Hỏi gì đó?");

        string url = _factory.Retrieval.DaGoi[0].RequestUri!.ToString();
        Assert.DoesNotContain("tenant", url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Retrieval_hong_thi_502_va_KHONG_goi_generation()
    {
        // Ba lần 503: hai cho lần gọi đầu + lần thử lại của TransientRetryHandler, một dư
        // ra để nếu handler thử nhiều hơn thì test vẫn chạy tới phần khẳng định số lần.
        _factory.Retrieval.TraVe(HttpStatusCode.ServiceUnavailable)
            .TraVe(HttpStatusCode.ServiceUnavailable)
            .TraVe(HttpStatusCode.ServiceUnavailable);

        using HttpClient client = _factory.TaoClientCuaTenant(_tenant);

        HttpResponseMessage phanHoi = await client.PostAsJsonAsync(
            new Uri("/chat/ask", UriKind.Relative), new { question = "Hỏi gì đó?" });

        Assert.Equal(HttpStatusCode.BadGateway, phanHoi.StatusCode);
        // Không có căn cứ thì không hỏi mô hình: một câu trả lời sinh ra từ hư không tệ
        // hơn hẳn một thông báo lỗi.
        Assert.Empty(_factory.Generation.DaGoi);
    }

    [Fact]
    public async Task Generation_hong_thi_502()
    {
        _factory.Retrieval.TraVe(HttpStatusCode.OK, _trangRetrieval);
        _factory.Generation.TraVe(HttpStatusCode.InternalServerError);

        using HttpClient client = _factory.TaoClientCuaTenant(_tenant);

        HttpResponseMessage phanHoi = await client.PostAsJsonAsync(
            new Uri("/chat/ask", UriKind.Relative), new { question = "Hỏi gì đó?" });

        Assert.Equal(HttpStatusCode.BadGateway, phanHoi.StatusCode);
    }

    [Fact]
    public async Task Retrieval_duoc_thu_lai_mot_lan_khi_gap_503()
    {
        _factory.Retrieval.TraVe(HttpStatusCode.ServiceUnavailable)
            .TraVe(HttpStatusCode.OK, _trangRetrieval);
        _factory.Generation.TraVe(HttpStatusCode.OK, _phanHoiGeneration);

        using HttpClient client = _factory.TaoClientCuaTenant(_tenant);
        ChatResponse? ketQua = await client.GetChatResponse("Hỏi gì đó?");

        Assert.NotNull(ketQua);
        Assert.Equal(2, _factory.Retrieval.DaGoi.Count);
    }

    [Fact]
    public async Task Generation_KHONG_duoc_thu_lai()
    {
        // POST không được thử lại: có thể tạo ra hai lần cùng một tác dụng phụ, và một lần
        // sinh chữ mất hàng chục giây nên thử lại chỉ nhân đôi thời gian người dùng chờ.
        _factory.Retrieval.TraVe(HttpStatusCode.OK, _trangRetrieval);
        _factory.Generation.TraVe(HttpStatusCode.ServiceUnavailable)
            .TraVe(HttpStatusCode.OK, _phanHoiGeneration);

        using HttpClient client = _factory.TaoClientCuaTenant(_tenant);

        HttpResponseMessage phanHoi = await client.PostAsJsonAsync(
            new Uri("/chat/ask", UriKind.Relative), new { question = "Hỏi gì đó?" });

        Assert.Equal(HttpStatusCode.BadGateway, phanHoi.StatusCode);
        Assert.Single(_factory.Generation.DaGoi);
    }

    [Fact]
    public async Task Cau_hoi_rong_thi_400()
    {
        using HttpClient client = _factory.TaoClientCuaTenant(_tenant);

        HttpResponseMessage phanHoi = await client.PostAsJsonAsync(
            new Uri("/chat/ask", UriKind.Relative), new { question = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, phanHoi.StatusCode);
        Assert.Empty(_factory.Retrieval.DaGoi);
    }
}

/// <summary>Rút gọn phần gọi lặp lại trong các test.</summary>
internal static class ChatClientExtensions
{
    public static async Task<ChatResponse?> GetChatResponse(this HttpClient client, string cauHoi)
    {
        HttpResponseMessage phanHoi = await client.PostAsJsonAsync(
            new Uri("/chat/ask", UriKind.Relative), new { question = cauHoi });

        phanHoi.EnsureSuccessStatusCode();
        return await phanHoi.Content.ReadFromJsonAsync<ChatResponse>();
    }
}
