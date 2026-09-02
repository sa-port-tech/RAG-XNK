using System.Net;
using Microsoft.Extensions.Logging;

namespace Xnk.Chat.Http;

/// <summary>
/// Thử lại một lần khi gặp lỗi tạm thời, và <b>chỉ với phương thức đọc</b>.
/// </summary>
/// <remarks>
/// <para>
/// Vì sao tự viết thay vì kéo một gói resilience: chỗ này cần đúng ba luật, và cả ba đều là
/// quyết định nghiệp vụ chứ không phải tham số kỹ thuật. Viết ra 40 dòng đọc được thì
/// người sau biết chính xác cái gì được thử lại; một handler tiêu chuẩn cấu hình sẵn thì
/// phải mở tài liệu của gói ra mới biết.
/// </para>
/// <list type="number">
/// <item>
/// <b>Chỉ GET.</b> Thử lại một POST là có thể tạo ra hai lần cùng một tác dụng phụ. Ở đây
/// nghĩa là: gọi lại <c>retrieval</c> thì an toàn, gọi lại <c>generation</c> thì không —
/// và cũng không đáng, vì một lần sinh chữ mất hàng chục giây.
/// </item>
/// <item>
/// <b>Chỉ lỗi tạm thời.</b> Lỗi mạng, 502/503/504, hoặc quá hạn. Một 400 hay 401 thử lại
/// bao nhiêu lần cũng vẫn thế, chỉ tốn thêm thời gian của người đang chờ.
/// </item>
/// <item>
/// <b>Đúng một lần.</b> Đủ để vượt qua một container vừa khởi động lại; nhiều hơn thì độ
/// trễ mà người dùng thấy nhân lên trong khi xác suất thành công tăng không đáng kể.
/// </item>
/// </list>
/// </remarks>
/// <param name="log">Ghi lại mỗi lần thử lại — im lặng thì không ai biết hệ thống đang chật vật.</param>
public sealed class TransientRetryHandler(ILogger<TransientRetryHandler> log) : DelegatingHandler
{
    /// <summary>Khoảng chờ trước lần thử thứ hai.</summary>
    public static readonly TimeSpan KhoangCho = TimeSpan.FromMilliseconds(200);

    private static readonly HttpStatusCode[] _maTamThoi =
    [
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout,
    ];

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Method != HttpMethod.Get)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        try
        {
            HttpResponseMessage phanHoi = await base.SendAsync(request, cancellationToken);
            if (!_maTamThoi.Contains(phanHoi.StatusCode))
            {
                return phanHoi;
            }

            log.LogWarning(
                "{Method} {Uri} trả {Code}, thử lại một lần.",
                request.Method, request.RequestUri, (int)phanHoi.StatusCode);
            phanHoi.Dispose();
        }
        catch (HttpRequestException loi)
        {
            log.LogWarning(loi, "{Method} {Uri} lỗi mạng, thử lại một lần.", request.Method, request.RequestUri);
        }
        catch (TaskCanceledException loi) when (!cancellationToken.IsCancellationRequested)
        {
            // Người gọi huỷ thì KHÔNG thử lại — chỉ thử lại khi chính client hết giờ.
            log.LogWarning(loi, "{Method} {Uri} quá hạn, thử lại một lần.", request.Method, request.RequestUri);
        }

        await Task.Delay(KhoangCho, cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
