using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xnk.IdentityTenant.Data;
using Xnk.Shared.Authentication;
using Xnk.Shared.Tenancy;

namespace Xnk.IdentityTenant.Security;

/// <summary>
/// Phát JWT cho một người dùng đã xác thực.
/// </summary>
/// <remarks>
/// <para>
/// Ba claim <c>tenant_id</c>, <c>tenant_type</c>, <c>role</c> lấy tên từ hằng trong
/// <see cref="TenantClaims"/> chứ không gõ chuỗi ở đây. Đó không phải chuyện gọn gàng: nếu
/// service này phát <c>tenant_id</c> còn corpus-service đọc <c>tenantId</c>, kết quả không
/// phải lỗi biên dịch mà là một service coi mọi request như không có tenant — và hậu quả
/// (người dùng không thấy dữ liệu của mình) trông giống hệt một lỗi nghiệp vụ.
/// </para>
/// <para>
/// Khoá ký lấy từ cùng <see cref="JwtOptions"/> mà sáu service kia dùng để kiểm tra. Đây
/// là mô hình khoá đối xứng của prototype, và nó có một hệ quả phải nói thẳng: **service
/// nào giữ khoá cũng tự phát được token hợp lệ**. Chấp nhận được ở local; xem ADR-014 về
/// đường chuyển sang khoá bất đối xứng trước khi có môi trường thật.
/// </para>
/// </remarks>
/// <param name="jwtOptions">Tham số issuer/audience/khoá ký, dùng chung toàn hệ thống.</param>
/// <param name="tokenOptions">Tham số riêng của việc phát token.</param>
public sealed class TokenIssuer(IOptions<JwtOptions> jwtOptions, IOptions<TokenIssuingOptions> tokenOptions)
{
    private readonly JwtOptions _jwt = jwtOptions.Value;
    private readonly TokenIssuingOptions _token = tokenOptions.Value;
    private readonly JsonWebTokenHandler _handler = new();

    /// <summary>Kết quả phát token.</summary>
    /// <param name="AccessToken">Chuỗi JWT đã ký.</param>
    /// <param name="ExpiresIn">Số giây còn hiệu lực, tính từ lúc phát.</param>
    public readonly record struct KetQua(string AccessToken, int ExpiresIn);

    /// <summary>Phát token cho một người dùng, kèm tenant của họ.</summary>
    /// <param name="nguoiDung">Người dùng đã qua bước kiểm mật khẩu.</param>
    /// <param name="tenant">Tenant mà người dùng thuộc về.</param>
    public KetQua Phat(User nguoiDung, Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(nguoiDung);
        ArgumentNullException.ThrowIfNull(tenant);

        DateTime batDau = DateTime.UtcNow;
        DateTime hetHan = batDau.AddMinutes(_token.LifetimeMinutes);

        var mota = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            IssuedAt = batDau,
            NotBefore = batDau,
            Expires = hetHan,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, nguoiDung.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, nguoiDung.Email),
                // jti để về sau thu hồi được từng token cụ thể mà không phải đổi khoá ký.
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(TenantClaims.TenantId, tenant.Id.ToString()),
                new Claim(TenantClaims.TenantType, tenant.Type),
                new Claim(TenantClaims.Role, nguoiDung.Role),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };

        return new KetQua(
            _handler.CreateToken(mota),
            (int)(hetHan - batDau).TotalSeconds);
    }
}
