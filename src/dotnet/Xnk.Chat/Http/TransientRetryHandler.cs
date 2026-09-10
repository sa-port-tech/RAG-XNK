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

    /// <summary>Ngân sách cho MỖI lần thử.</summary>
    /// <remarks>
    /// <para>
    /// ⚠️ Đây là thứ làm cho nhánh "quá hạn thì thử lại" tồn tại thật.
    /// </para>
    /// <para>
    /// Bản trước bắt <c>TaskCanceledException</c> với bộ lọc
    /// <c>when (!cancellationToken.IsCancellationRequested)</c>. Bộ lọc đó
    /// <b>không bao giờ đúng</b>: <see cref="HttpClient.Timeout"/> được cài đặt bằng cách
    /// liên kết token của người gọi vào một CTS nội bộ, và token mà handler nhận CHÍNH LÀ
    /// token đã liên kết đó. Hết giờ thì nó đã bị huỷ; người gọi bỏ đi thì nó cũng bị huỷ.
    /// Hai trường hợp, một tín hiệu, và nhánh retry-sau-quá-hạn là mã chết.
    /// </para>
    /// <para>
    /// Nay mỗi lần thử chạy dưới một CTS RIÊNG. Khi CTS đó hết giờ mà token bên ngoài vẫn
    /// sống, ta biết chắc đây là "lần gọi này chậm" chứ không phải "người gọi đã bỏ đi" —
    /// và chỉ khi đó mới thử lại.
    /// </para>
    /// <para>
    /// 4 giây: hai lần thử cộng khoảng chờ vẫn nằm gọn trong
    /// <c>RetrievalTimeoutSeconds</c> mặc định 10 giây, nên trần của người gọi mới là thứ
    /// chốt hạ chứ không phải một cuộc đua giữa hai bộ đếm.
    /// </para>
    /// </remarks>
    public static readonly TimeSpan NganSachMoiLan = TimeSpan.FromSeconds(4);

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

        using (var lanDau = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            lanDau.CancelAfter(NganSachMoiLan);

            try
            {
                HttpResponseMessage phanHoi = await base.SendAsync(request, lanDau.Token);
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
            catch (OperationCanceledException loi) when (!cancellationToken.IsCancellationRequested)
            {
                // Ngân sách của LẦN NÀY hết, nhưng người gọi vẫn đang chờ, nên thử lại.
                // Người gọi bỏ đi thì cancellationToken đã bị huỷ, bộ lọc thành false, và
                // ngoại lệ đi thẳng ra ngoài — không thử lại, đúng như ý đồ ban đầu.
                log.LogWarning(loi, "{Method} {Uri} quá hạn, thử lại một lần.", request.Method, request.RequestUri);
            }
        }

        await Task.Delay(KhoangCho, cancellationToken);

        using var lanHai = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lanHai.CancelAfter(NganSachMoiLan);
        return await base.SendAsync(request, lanHai.Token);
    }
}
