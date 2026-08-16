using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Xnk.Shared.Authentication;

/// <summary>
/// Đăng ký xác thực JWT giống hệt nhau ở mọi service.
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// Thêm xác thực JWT bearer theo section <c>Jwt</c>.
    /// </summary>
    /// <remarks>
    /// Vì sao tập trung ở thư viện chung thay vì để mỗi service tự gọi
    /// <c>AddJwtBearer</c>: các tham số kiểm tra bên dưới là một tập hợp, và bỏ sót một
    /// cái không gây lỗi biên dịch, không làm đỏ test, chỉ làm token giả được chấp nhận.
    /// Bảy service tự cấu hình bảy lần thì xác suất một chỗ lỏng hơn phần còn lại là rất
    /// cao, và không ai phát hiện được điều đó bằng cách đọc diff của một PR.
    /// </remarks>
    public static IServiceCollection AddXnkJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // ValidateOnStart: cấu hình thiếu hoặc khoá quá ngắn làm service chết ngay lúc
        // khởi động, kèm thông báo nói rõ trường nào. Không có nó thì lỗi chỉ lộ ra ở
        // request đầu tiên có token — tức là sau khi deploy đã báo thành công.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        JwtOptions options = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? new JwtOptions();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(options.SigningKey)),
                    ValidateLifetime = true,
                    // Mặc định của thư viện là 5 phút. Với token ngắn hạn thì 5 phút ân
                    // hạn là một khoảng thời gian dài đáng kể sau khi token đã hết hạn.
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorization();

        return services;
    }
}
