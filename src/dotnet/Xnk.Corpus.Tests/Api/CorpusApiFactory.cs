using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xnk.Shared.Tenancy;

namespace Xnk.Corpus.Tests.Api;

/// <summary>
/// Dựng corpus-service thật trong tiến trình test và gọi nó qua HTTP.
/// </summary>
/// <remarks>
/// <para>
/// Vì sao phải là ứng dụng thật chứ không gọi thẳng handler: thứ cần chứng minh nằm ở
/// những lớp mà một unit test đi vòng qua — middleware xác thực đọc token, ngữ cảnh tenant
/// dựng từ claim, rồi global query filter đọc ngữ cảnh đó. Gọi thẳng hàm handler thì cả
/// ba lớp ấy không chạy, và test vẫn xanh kể cả khi xác thực bị tắt.
/// </para>
/// <para>
/// Token trong test được ký tại chỗ bằng cùng khoá mà ứng dụng dùng để kiểm tra, **không**
/// gọi sang identity-tenant. Test của corpus phải trả lời "corpus xử lý đúng chưa khi nhận
/// một token hợp lệ", chứ không kéo theo một service khác — kéo vào thì một lỗi ở
/// identity-tenant sẽ làm đỏ bộ test của corpus, và người đọc mất thời gian tìm nhầm chỗ.
/// </para>
/// </remarks>
/// <param name="connectionString">PostgreSQL dùng cho test, do <c>PostgresFixture</c> cấp.</param>
public sealed class CorpusApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    /// <summary>Khoá ký dùng trong test. Tối thiểu 32 ký tự theo ràng buộc của JwtOptions.</summary>
    public const string KhoaKy = "khoa-ky-danh-rieng-cho-test-dai-32-ky-tu";

    /// <summary>Issuer dùng trong test.</summary>
    public const string Issuer = "https://identity.test.local";

    /// <summary>Audience dùng trong test.</summary>
    public const string Audience = "xnk-api-test";

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Môi trường Production để appsettings.Development.json KHÔNG được nạp — nếu nạp,
        // chuỗi kết nối của máy dev sẽ đè lên chuỗi của test và bộ test âm thầm chạy trên
        // database thật của người đang code.
        builder.UseEnvironment(Environments.Production);

        builder.UseSetting("ConnectionStrings:Corpus", connectionString);
        builder.UseSetting("Jwt:Issuer", Issuer);
        builder.UseSetting("Jwt:Audience", Audience);
        builder.UseSetting("Jwt:SigningKey", KhoaKy);
    }

    /// <summary>
    /// Tạo một client mang token của một tenant cụ thể.
    /// </summary>
    /// <param name="tenantId">Tenant mà token đại diện.</param>
    /// <param name="vaiTro">Vai trò ghi vào claim.</param>
    public HttpClient TaoClientCuaTenant(Guid tenantId, string vaiTro = "user")
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TaoToken(tenantId, vaiTro));
        return client;
    }

    private static string TaoToken(Guid tenantId, string vaiTro)
    {
        var mota = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddMinutes(10),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim(TenantClaims.TenantId, tenantId.ToString()),
                new Claim(TenantClaims.TenantType, "b2b_khach_hang"),
                new Claim(TenantClaims.Role, vaiTro),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KhoaKy)),
                SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(mota);
    }
}
