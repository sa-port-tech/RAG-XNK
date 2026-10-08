using System.ComponentModel.DataAnnotations;

namespace Xnk.IdentityTenant.Security;

/// <summary>
/// Tham số phát token, đọc từ section <c>Token</c>.
/// </summary>
/// <remarks>
/// Tách khỏi <see cref="Xnk.Shared.Authentication.JwtOptions"/> có chủ đích: <c>JwtOptions</c>
/// là thứ **mọi** service cần để KIỂM TRA token, còn tuổi thọ token chỉ có ý nghĩa với
/// service PHÁT ra nó. Nhét vào lớp dùng chung thì sáu service khác mang theo một tham số
/// chúng không bao giờ dùng, và ai đọc cũng phải mất một lúc để hiểu vì sao.
/// </remarks>
public sealed class TokenIssuingOptions
{
    /// <summary>Tên section trong appsettings.</summary>
    public const string SectionName = "Token";

    /// <summary>
    /// Tuổi thọ token tính bằng phút.
    /// </summary>
    /// <remarks>
    /// Mặc định 60 phút. Ngắn hơn thì phiên làm việc của người tra cứu tại cảng bị ngắt
    /// giữa chừng; dài hơn thì một token rò rỉ sống quá lâu. Prototype chưa có refresh
    /// token — khi có, con số này nên tụt xuống hàng phút.
    /// </remarks>
    [Range(1, 24 * 60)]
    public int LifetimeMinutes { get; init; } = 60;
}
