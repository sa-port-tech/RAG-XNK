using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xnk.Shared.Tenancy;

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
    /// <para>
    /// Vì sao tập trung ở thư viện chung thay vì để mỗi service tự gọi
    /// <c>AddJwtBearer</c>: các tham số kiểm tra bên dưới là một tập hợp, và bỏ sót một
    /// cái không gây lỗi biên dịch, không làm đỏ test, chỉ làm token giả được chấp nhận.
    /// Bảy service tự cấu hình bảy lần thì xác suất một chỗ lỏng hơn phần còn lại là rất
    /// cao, và không ai phát hiện được điều đó bằng cách đọc diff của một PR.
    /// </para>
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

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // ⚠️ Cấu hình qua IOptions<JwtOptions>, KHÔNG đọc lại configuration lần thứ hai.
        //
        // Bản trước gọi `configuration.GetSection(...).Get<JwtOptions>() ?? new JwtOptions()`
        // ngay tại đây và dựng TokenValidationParameters từ đối tượng CHƯA QUA VALIDATE đó.
        // Hôm nay nó vẫn đúng nhờ ValidateOnStart giết tiến trình trước khi có request đầu
        // tiên. Nhưng "đúng nhờ một dòng ở chỗ khác" là loại đúng sẽ hỏng lặng lẽ: gỡ
        // ValidateOnStart đi thì service khởi động bình thường với issuer rỗng, audience
        // rỗng và khoá rỗng — không lỗi biên dịch, không test đỏ.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((jwt, nguon) =>
            {
                JwtOptions options = nguon.Value;

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
                    // Chỉ HS256. Không để thư viện suy thuật toán từ header của chính
                    // token — đó là đường dẫn tới "alg: none" và tới việc nhầm khoá công
                    // khai thành khoá HMAC. Phía Python khai cùng điều này ở `THUAT_TOAN`.
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    // Mặc định của thư viện là 5 phút. Với token ngắn hạn thì 5 phút ân
                    // hạn là một khoảng thời gian dài đáng kể sau khi token đã hết hạn.
                    ClockSkew = TimeSpan.FromSeconds(30),
                };

                jwt.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (!CoDuClaimBatBuoc(context.Principal))
                        {
                            context.Fail(
                                "Token thiếu claim bắt buộc (sub, tenant_id) hoặc tenant_id không phải GUID.");
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Token phải mang <c>sub</c> và một <c>tenant_id</c> đúng dạng GUID.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Đây là bản đối ứng của <c>CLAIM_BAT_BUOC</c> trong <c>retrieval/auth.py</c>. Hai danh
    /// sách phải giống nhau; lệch là một service chấp nhận thứ service bên cạnh từ chối, và
    /// không phép kiểm nào trong repo bắt được điều đó.
    /// </para>
    /// <para>
    /// Vì sao chặn ở TẦNG XÁC THỰC chứ không ở <see cref="HttpTenantContext"/>: chỗ đó trả
    /// <c>null</c> cho claim thiếu hoặc méo, và <c>null</c> được global query filter đọc là
    /// "chỉ thấy tài liệu dùng chung". Hướng đó fail-closed nên không rò rỉ, nhưng nó **im
    /// lặng** — người gọi nhận 200 với danh sách ngắn hơn họ tưởng và không ai biết token
    /// đã hỏng. Một tài khoản mất quyền xem trong im lặng được báo sau nhiều ngày, mô tả là
    /// "hệ thống thiếu dữ liệu", và không ai đi tìm ở tầng xác thực.
    /// </para>
    /// <para>
    /// <c>identity-tenant</c> LUÔN phát cả hai claim (<c>TokenIssuer.Phat</c>). Một token
    /// hợp lệ về chữ ký mà thiếu chúng không phải thứ hệ thống này sinh ra.
    /// </para>
    /// </remarks>
    private static bool CoDuClaimBatBuoc(ClaimsPrincipal? nguoiDung)
    {
        if (nguoiDung is null)
        {
            return false;
        }

        // `sub` bị ánh xạ sang ClaimTypes.NameIdentifier khi MapInboundClaims bật (mặc
        // định). Kiểm cả hai tên để phép kiểm không phụ thuộc vào cờ đó — đổi cờ là việc
        // một dòng, và không ai nghĩ nó chạm tới xác thực.
        bool coSub =
            nguoiDung.FindFirst(JwtRegisteredClaimNames.Sub) is not null
            || nguoiDung.FindFirst(ClaimTypes.NameIdentifier) is not null;

        string? tenant = nguoiDung.FindFirst(TenantClaims.TenantId)?.Value;

        return coSub && Guid.TryParse(tenant, out _);
    }
}
