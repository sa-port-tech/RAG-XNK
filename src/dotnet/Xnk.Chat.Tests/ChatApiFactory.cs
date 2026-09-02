using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xnk.Chat.Clients;
using Xnk.Shared.Tenancy;

namespace Xnk.Chat.Tests;

/// <summary>
/// Ghi lại mọi request đi ra và trả về phản hồi đã đặt trước.
/// </summary>
/// <remarks>
/// Thay ở tầng <see cref="HttpMessageHandler"/> chứ không thay <c>RetrievalClient</c> /
/// <c>GenerationClient</c>: hai lớp đó chỉ là phần dễ, còn thứ cần kiểm nằm ở **chuỗi
/// handler** — chuyển tiếp <c>Authorization</c> và thử lại. Thay client thì hai handler ấy
/// không chạy, và test xanh kể cả khi chúng bị gỡ.
/// </remarks>
public sealed class GhiLaiHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _kichBan = new();

    /// <summary>Các request đã đi qua, theo thứ tự.</summary>
    public List<HttpRequestMessage> DaGoi { get; } = [];

    /// <summary>Đặt phản hồi cho lần gọi kế tiếp.</summary>
    public GhiLaiHandler TraVe(HttpStatusCode ma, string? json = null)
    {
        _kichBan.Enqueue(_ => new HttpResponseMessage(ma)
        {
            Content = new StringContent(json ?? "{}", Encoding.UTF8, "application/json"),
        });
        return this;
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        DaGoi.Add(request);

        // Hết kịch bản thì 500 — im lặng trả 200 rỗng sẽ biến một test thiếu kịch bản
        // thành một test xanh vô nghĩa.
        Func<HttpRequestMessage, HttpResponseMessage> tiep = _kichBan.Count > 0
            ? _kichBan.Dequeue()
            : _ => new HttpResponseMessage(HttpStatusCode.InternalServerError);

        return Task.FromResult(tiep(request));
    }
}

/// <summary>
/// Dựng chat-service thật trong tiến trình test, với hai service phía sau được thay bằng
/// <see cref="GhiLaiHandler"/>.
/// </summary>
public sealed class ChatApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Khoá ký dùng trong test.</summary>
    public const string KhoaKy = "khoa-ky-danh-rieng-cho-test-dai-32-ky-tu";

    /// <summary>Issuer dùng trong test.</summary>
    public const string Issuer = "https://identity.test.local";

    /// <summary>Audience dùng trong test.</summary>
    public const string Audience = "xnk-api-test";

    /// <summary>Handler đứng thay <c>retrieval</c>.</summary>
    public GhiLaiHandler Retrieval { get; } = new();

    /// <summary>Handler đứng thay <c>generation</c>.</summary>
    public GhiLaiHandler Generation { get; } = new();

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Production);

        builder.UseSetting("Jwt:Issuer", Issuer);
        builder.UseSetting("Jwt:Audience", Audience);
        builder.UseSetting("Jwt:SigningKey", KhoaKy);
        builder.UseSetting("Downstream:RetrievalBaseUrl", "http://retrieval.test");
        builder.UseSetting("Downstream:GenerationBaseUrl", "http://generation.test");

        builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient(TenClient.Retrieval)
                .ConfigurePrimaryHttpMessageHandler(() => Retrieval);
            services.AddHttpClient(TenClient.Generation)
                .ConfigurePrimaryHttpMessageHandler(() => Generation);
        });
    }

    /// <summary>Tạo client mang token của một tenant.</summary>
    public HttpClient TaoClientCuaTenant(Guid tenantId)
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TaoToken(tenantId));
        return client;
    }

    /// <summary>Chuỗi token thô, để test đối chiếu với header đã chuyển tiếp.</summary>
    public string TaoToken(Guid tenantId)
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
                new Claim(TenantClaims.TenantType, "noi_bo"),
                new Claim(TenantClaims.Role, "user"),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KhoaKy)),
                SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(mota);
    }
}
