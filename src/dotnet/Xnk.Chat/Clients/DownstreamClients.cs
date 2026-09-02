using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using Xnk.Chat.Contracts;

namespace Xnk.Chat.Clients;

/// <summary>
/// Địa chỉ và thời gian chờ của các service phía sau, đọc từ section <c>Downstream</c>.
/// </summary>
public sealed class DownstreamOptions
{
    /// <summary>Tên section trong appsettings.</summary>
    public const string SectionName = "Downstream";

    /// <summary>Địa chỉ gốc của <c>retrieval</c>, ví dụ <c>http://retrieval:8000</c>.</summary>
    [Required(AllowEmptyStrings = false)]
    [Url]
    public string RetrievalBaseUrl { get; init; } = string.Empty;

    /// <summary>Địa chỉ gốc của <c>generation</c>, ví dụ <c>http://generation:8000</c>.</summary>
    [Required(AllowEmptyStrings = false)]
    [Url]
    public string GenerationBaseUrl { get; init; } = string.Empty;

    /// <summary>Thời gian chờ khi gọi <c>retrieval</c>, tính bằng giây.</summary>
    /// <remarks>Một truy vấn SQL có chỉ mục; chờ lâu hơn thế nghĩa là có chuyện khác.</remarks>
    [Range(1, 60)]
    public int RetrievalTimeoutSeconds { get; init; } = 10;

    /// <summary>Thời gian chờ khi gọi <c>generation</c>, tính bằng giây.</summary>
    /// <remarks>
    /// Rộng hơn hẳn: một lần sinh chữ trên CPU của máy dev mất hàng chục giây. Vẫn hữu hạn,
    /// vì một request treo vô hạn giữ kết nối và cuối cùng làm nghẽn cả service.
    /// </remarks>
    [Range(1, 600)]
    public int GenerationTimeoutSeconds { get; init; } = 120;

    /// <summary>Số văn bản tối đa lấy làm ngữ cảnh cho một câu hỏi.</summary>
    /// <remarks>
    /// Nhỏ có chủ đích. Nhồi cả trăm văn bản vào prompt không làm câu trả lời đúng hơn —
    /// nó làm mô hình loãng và làm chi phí tăng tuyến tính. Con số thật do epic E3 chốt
    /// bằng đo đạc trên golden set (docs/14), không phải bằng cảm giác.
    /// </remarks>
    [Range(1, 50)]
    public int MaxContextDocuments { get; init; } = 10;
}

/// <summary>Tên các <see cref="HttpClient"/> đã đăng ký.</summary>
public static class TenClient
{
    /// <summary>Client gọi <c>retrieval</c>.</summary>
    public const string Retrieval = "retrieval";

    /// <summary>Client gọi <c>generation</c>.</summary>
    public const string Generation = "generation";
}

/// <summary>Gọi <c>retrieval</c> để lấy văn bản làm căn cứ.</summary>
/// <param name="httpClientFactory">Nguồn client đã cấu hình sẵn handler và thời gian chờ.</param>
public sealed class RetrievalClient(IHttpClientFactory httpClientFactory)
{
    /// <summary>Lấy các văn bản mà người dùng hiện tại được xem.</summary>
    /// <remarks>
    /// ⚠️ Không truyền tenant nào ở đây, và đó là **đúng**: <c>retrieval</c> tự đọc claim
    /// <c>tenant_id</c> từ token mà <c>ForwardAuthorizationHandler</c> chuyển tiếp. Nếu
    /// chat gửi kèm một tham số tenant, hệ thống sẽ có hai nguồn sự thật cho cùng một điều
    /// — và nguồn dễ giả mạo hơn nằm ở phía người gọi.
    /// </remarks>
    public async Task<IReadOnlyList<Citation>> LayCanCuAsync(int soLuong, CancellationToken huy)
    {
        using HttpClient client = httpClientFactory.CreateClient(TenClient.Retrieval);

        RetrievalPage? trang = await client.GetFromJsonAsync<RetrievalPage>(
            $"/retrieval/documents?page_size={soLuong}", huy);

        return trang is null
            ? []
            : [.. trang.Items.Select(d => new Citation(d.DocumentNumber, d.Title, d.IsShared))];
    }
}

/// <summary>Gọi <c>generation</c> để sinh câu trả lời.</summary>
/// <param name="httpClientFactory">Nguồn client đã cấu hình sẵn handler và thời gian chờ.</param>
public sealed class GenerationClient(IHttpClientFactory httpClientFactory)
{
    /// <summary>Sinh câu trả lời từ câu hỏi và các căn cứ đã lấy được.</summary>
    public async Task<GenerationResult> TraLoiAsync(
        string cauHoi,
        IReadOnlyList<Citation> canCu,
        CancellationToken huy)
    {
        using HttpClient client = httpClientFactory.CreateClient(TenClient.Generation);

        // Ngữ cảnh gửi đi là số hiệu + trích yếu. Nội dung điều khoản chưa có trong hệ
        // thống (corpus mới chỉ có metadata), nên gửi nhiều hơn thế là gửi thứ không tồn tại.
        var yeuCau = new GenerationRequest(
            cauHoi,
            [.. canCu.Select(c => $"{c.DocumentNumber} — {c.Title}")]);

        HttpResponseMessage phanHoi = await client.PostAsJsonAsync("/generation/answer", yeuCau, huy);
        phanHoi.EnsureSuccessStatusCode();

        GenerationResponse? ketQua = await phanHoi.Content.ReadFromJsonAsync<GenerationResponse>(huy);

        return ketQua is null
            ? throw new InvalidOperationException("generation trả về thân rỗng.")
            : new GenerationResult(ketQua.Answer, ketQua.Model);
    }

    /// <summary>Kết quả sinh câu trả lời.</summary>
    /// <param name="Answer">Nội dung câu trả lời.</param>
    /// <param name="Model">Tên mô hình đã sinh ra nó.</param>
    public readonly record struct GenerationResult(string Answer, string Model);
}
