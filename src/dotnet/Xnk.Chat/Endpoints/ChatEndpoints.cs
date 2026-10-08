using Microsoft.Extensions.Options;
using Xnk.Chat.Clients;
using Xnk.Chat.Contracts;

namespace Xnk.Chat.Endpoints;

/// <summary>
/// Endpoint hỏi–đáp.
/// </summary>
public static class ChatEndpoints
{
    /// <summary>Gắn nhóm endpoint hội thoại vào ứng dụng.</summary>
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/ask", Hoi)
            // Đặt ở cấp endpoint vì nhóm này hiện chỉ có một route; thêm route thứ hai thì
            // chuyển sang MapGroup().RequireAuthorization() như bên corpus.
            .RequireAuthorization()
            .WithName("HoiDap")
            .WithSummary("Hỏi một câu nghiệp vụ, nhận câu trả lời kèm căn cứ")
            .WithTags("chat")
            .Produces<ChatResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesValidationProblem();

        return endpoints;
    }

    /// <remarks>
    /// Trình tự cố định: <c>retrieval</c> trước, <c>generation</c> sau. Không song song hoá —
    /// câu trả lời phụ thuộc vào ngữ cảnh, nên không có gì để làm song song.
    /// <para>
    /// Cả hai lời gọi đều mang token của **người dùng**, không phải token hệ thống — xem
    /// <see cref="Http.ForwardAuthorizationHandler"/>. Đó là điều giữ cho lớp cách ly tenant
    /// còn tác dụng ở chặng thứ hai.
    /// </para>
    /// </remarks>
    private static async Task<IResult> Hoi(
        ChatRequest yeuCau,
        RetrievalClient retrieval,
        GenerationClient generation,
        IOptions<DownstreamOptions> options,
        ILogger<ChatRequest> log,
        CancellationToken huy)
    {
        if (string.IsNullOrWhiteSpace(yeuCau.Question))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["question"] = ["Câu hỏi không được để trống."],
            });
        }

        IReadOnlyList<TaiLieuThamKhao> canCu;
        try
        {
            canCu = await retrieval.LayTaiLieuThamKhaoAsync(options.Value.MaxContextDocuments, huy);
        }
        catch (OperationCanceledException) when (huy.IsCancellationRequested)
        {
            // Người gọi đóng tab. KHÔNG phải sự cố của retrieval.
            //
            // Bản trước gộp ca này vào khối 502 bên dưới, nên mỗi lần một người dùng bỏ đi
            // giữa chừng là một dòng LogError đổ lỗi cho một service hoàn toàn khoẻ mạnh,
            // và người trực đêm được chỉ sang đúng chỗ không có gì để xem. Với một mô hình
            // mất 20-60 giây mỗi câu trả lời, người dùng bỏ đi giữa chừng KHÔNG hiếm.
            //
            // 499 là mã nginx dùng cho "client closed request". Không phải mã chuẩn IANA,
            // nhưng là mã mà lớp proxy ngay phía trước đã dùng cho đúng tình huống này,
            // nên log của hai tầng đọc chung được.
            log.LogInformation("Người gọi huỷ trước khi retrieval trả lời.");
            return TypedResults.StatusCode(499);
        }
        catch (Exception loi) when (loi is HttpRequestException or TaskCanceledException)
        {
            // 502 chứ không phải 500: lỗi nằm ở service phía sau. Phân biệt được hai loại
            // đó là thứ giúp người trực đêm biết nên đi xem log của ai.
            log.LogError(loi, "Không gọi được retrieval.");
            return TypedResults.Problem(
                title: "Không lấy được căn cứ",
                detail: "Service truy xuất không phản hồi.",
                statusCode: StatusCodes.Status502BadGateway);
        }

        GenerationClient.GenerationResult ketQua;
        try
        {
            ketQua = await generation.TraLoiAsync(yeuCau.Question, canCu, huy);
        }
        catch (OperationCanceledException) when (huy.IsCancellationRequested)
        {
            // Cùng lý do như khối retrieval phía trên, và ở đây còn hay xảy ra hơn: sinh
            // chữ là bước lâu nhất của cả đường ống.
            log.LogInformation("Người gọi huỷ trước khi generation trả lời.");
            return TypedResults.StatusCode(499);
        }
        catch (Exception loi) when (loi is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            log.LogError(loi, "Không gọi được generation.");
            return TypedResults.Problem(
                title: "Không sinh được câu trả lời",
                detail: "Service sinh câu trả lời không phản hồi.",
                statusCode: StatusCodes.Status502BadGateway);
        }

        // Căn cứ trả kèm LUÔN, kể cả khi rỗng: một câu trả lời không có căn cứ nào mà trông
        // giống hệt câu có căn cứ là đúng thứ hệ thống này tồn tại để tránh.
        return TypedResults.Ok(new ChatResponse(ketQua.Answer, ketQua.Model, canCu));
    }
}
