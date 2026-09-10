using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
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
/// <param name="moiTruong">Nguồn origin của chính ứng dụng này.</param>
public sealed class TokenHandler(PhienDangNhap phien, IWebAssemblyHostEnvironment moiTruong)
    : DelegatingHandler
{
    private readonly Uri _goc = new(moiTruong.BaseAddress);

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (phien.DaDangNhap && CungGoc(request.RequestUri))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", phien.Token);
        }

        return base.SendAsync(request, cancellationToken);
    }

    /// <summary>Đích của request có cùng origin với chính trang này không.</summary>
    /// <remarks>
    /// <para>
    /// Bản trước gắn token vào <b>mọi</b> request mà client này gửi, không hỏi nó đi đâu.
    /// Hôm nay chỉ có một origin, nên nó đúng — nhưng đó là tính chất của HÔM NAY, không
    /// phải của mã. Ngày ai đó gọi một API bản đồ, một CDN phông chữ, hay một webhook từ
    /// cùng client này, token của người dùng đi kèm sang bên thứ ba mà không có gì trong
    /// diff trông đáng ngờ.
    /// </para>
    /// <para>
    /// Địa chỉ tương đối được chấp nhận: chúng luôn phân giải theo <c>BaseAddress</c>, tức
    /// chính origin này.
    /// </para>
    /// </remarks>
    private bool CungGoc(Uri? dich)
    {
        if (dich is null || !dich.IsAbsoluteUri)
        {
            return true;
        }

        return Uri.Compare(
            dich,
            _goc,
            UriComponents.SchemeAndServer,
            UriFormat.Unescaped,
            StringComparison.OrdinalIgnoreCase) == 0;
    }
}
