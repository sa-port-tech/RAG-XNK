using System.ComponentModel.DataAnnotations;

namespace Xnk.Shared.Authentication;

/// <summary>
/// Tham số xác thực JWT, đọc từ section <c>Jwt</c> của cấu hình.
/// </summary>
/// <remarks>
/// Prototype dùng JWT đối xứng cho cả ba loại tenant; production chuyển sang SSO
/// SAML/OIDC nhưng giữ nguyên mô hình <c>tenant_id</c> (docs/00 §12).
/// <para>
/// <see cref="SigningKey"/> KHÔNG được có giá trị mặc định trong mã nguồn. Một khoá mặc
/// định là khoá sẽ đi thẳng lên production vào một ngày nào đó, và không ai nhớ nó tồn
/// tại. Thiếu cấu hình thì service phải chết lúc khởi động — xem
/// <see cref="JwtAuthenticationExtensions"/>.
/// </para>
/// </remarks>
public sealed class JwtOptions
{
    /// <summary>Tên section trong appsettings.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Bên phát hành token — identity-tenant.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Đối tượng nhận token.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Khoá ký đối xứng. Lấy từ Secrets Manager khi chạy thật, từ biến môi trường khi
    /// chạy local. Tối thiểu 32 byte cho HMAC-SHA256.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [MinLength(32, ErrorMessage = "Khoá ký JWT phải dài tối thiểu 32 ký tự cho HMAC-SHA256.")]
    public string SigningKey { get; init; } = string.Empty;
}
