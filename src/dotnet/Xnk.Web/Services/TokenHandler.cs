using System.Net.Http.Headers;

namespace Xnk.Web.Services;

/// <summary>
/// Gắn token vào mọi request đi ra.
/// </summary>
/// <remarks>
/// Đặt ở tầng handler chứ không gán tay ở từng lời gọi: một chỗ quên gán là một màn hình
/// trả 401 mà không ai hiểu vì sao, và chỗ quên đó trông y hệt mã bình thường.
/// </remarks>
/// <param name="phien">Phiên đăng nhập hiện tại.</param>
public sealed class TokenHandler(PhienDangNhap phien) : DelegatingHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (phien.DaDangNhap)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", phien.Token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
