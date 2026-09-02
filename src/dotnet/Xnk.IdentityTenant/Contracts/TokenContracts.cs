using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Xnk.IdentityTenant.Contracts;

/// <summary>Thân yêu cầu phát token.</summary>
/// <param name="Email">Email đăng nhập.</param>
/// <param name="Password">Mật khẩu dạng chữ thường.</param>
public sealed record TokenRequest(
    [property: Required(AllowEmptyStrings = false)]
    [property: EmailAddress]
    [property: MaxLength(320)]
    string Email,
    [property: Required(AllowEmptyStrings = false)]
    [property: MaxLength(256)]
    string Password);

/// <summary>Thân phản hồi khi phát token thành công.</summary>
/// <param name="AccessToken">Chuỗi JWT đã ký.</param>
/// <param name="TokenType">Luôn là <c>Bearer</c>.</param>
/// <param name="ExpiresIn">Số giây còn hiệu lực.</param>
/// <remarks>
/// Tên trường theo RFC 6749 §5.1 (<c>access_token</c>, <c>token_type</c>, <c>expires_in</c>)
/// để client dùng được thư viện OAuth có sẵn thay vì phải viết bộ phân tích riêng. Quy ước
/// đặt tên snake_case cho JSON của riêng endpoint này được khai ngay tại chỗ ánh xạ, không
/// đổi quy ước JSON của cả service.
/// </remarks>
public sealed record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);
