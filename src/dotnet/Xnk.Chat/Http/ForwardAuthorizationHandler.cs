using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Xnk.Chat.Http;

/// <summary>
/// Chuyển tiếp header <c>Authorization</c> của người dùng sang service phía sau.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ Đây là mắt xích giữ cho lớp cách ly tenant còn tác dụng ở chặng thứ hai.
/// </para>
/// <para>
/// <c>retrieval</c> lọc dữ liệu theo claim <c>tenant_id</c> trong token. Nếu chat gọi nó
/// bằng một token hệ thống — hoặc không token — thì mọi câu hỏi của mọi tenant đều chạy
/// dưới cùng một danh tính, và bộ lọc ở tầng SQL trở thành vô nghĩa dù mã của nó vẫn đúng
/// từng chữ. Lỗ hổng loại đó không hiện ra trong diff của <c>retrieval</c>, cũng không
/// hiện ra trong test của nó.
/// </para>
/// <para>
/// Cài bằng <see cref="DelegatingHandler"/> chứ không gán thủ công ở từng lời gọi: một chỗ
/// quên gán là một chỗ rò rỉ, và chỗ quên đó trông y hệt mã bình thường.
/// </para>
/// </remarks>
/// <param name="httpContextAccessor">Truy cập request đang xử lý.</param>
public sealed class ForwardAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    : DelegatingHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? header = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrWhiteSpace(header)
            && AuthenticationHeaderValue.TryParse(header, out AuthenticationHeaderValue? parsed))
        {
            request.Headers.Authorization = parsed;
        }

        // Thiếu header thì KHÔNG tự bịa ra một danh tính nào — cứ để service phía sau trả
        // 401. Điền một token mặc định ở đây là mở đúng cánh cửa mà lớp này sinh ra để đóng.
        return base.SendAsync(request, cancellationToken);
    }
}
