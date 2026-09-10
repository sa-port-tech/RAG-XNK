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
    private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>
        _kichBan = new();

    /// <summary>Các request đã đi qua, theo thứ tự.</summary>
    public List<HttpRequestMessage> DaGoi { get; } = [];

    /// <summary>Đặt phản hồi cho lần gọi kế tiếp.</summary>
    /// <param name="ma">Mã trạng thái trả về.</param>
    /// <param name="json">Thân phản hồi.</param>
    /// <param name="duongDanMongDoi">
    /// Nếu đặt, bước này <b>kiểm</b> đường dẫn của request trước khi trả lời.
    /// </param>
    /// <remarks>
    /// Vì sao có <paramref name="duongDanMongDoi"/>: kịch bản là một hàng đợi, và hàng đợi
    /// không nhìn request. Một lần thử lại ngoài dự kiến ăn mất một phản hồi, rồi **mọi**
    /// khẳng định sau đó lệch đi một bước — test vẫn chạy, vẫn cho kết quả, chỉ là kết quả
    /// của một kịch bản khác. Khai đường dẫn mong đợi biến lệch pha im lặng thành một lỗi
    /// nói rõ nó lệch ở đâu.
    /// </remarks>
    public GhiLaiHandler TraVe(HttpStatusCode ma, string? json = null, string? duongDanMongDoi = null)
    {
        _kichBan.Enqueue((request, _) =>
        {
            KiemDuongDan(request, duongDanMongDoi);
            return Task.FromResult(new HttpResponseMessage(ma)
            {
                Content = new StringContent(json ?? "{}", Encoding.UTF8, "application/json"),
            });
        });
        return this;
    }

    /// <summary>Lần gọi kế tiếp <b>chờ</b> rồi mới trả lời.</summary>
    /// <remarks>
    /// Cần thiết để chạm tới nhánh "quá hạn thì thử lại" của
    /// <c>TransientRetryHandler</c>. Trước đây kịch bản chỉ biết trả
    /// <c>Task.FromResult</c>, nên không test nào làm cho một lần gọi CHẬM được — và nhánh
    /// đó vừa là mã chết vừa không có test, hai điều che nhau.
    /// </remarks>
    public GhiLaiHandler ChoRoiTraVe(TimeSpan cho, HttpStatusCode ma, string? json = null)
    {
        _kichBan.Enqueue(async (_, huy) =>
        {
            await Task.Delay(cho, huy);
            return new HttpResponseMessage(ma)
            {
                Content = new StringContent(json ?? "{}", Encoding.UTF8, "application/json"),
            };
        });
        return this;
    }

    /// <summary>Lần gọi kế tiếp <b>ném</b> ngoại lệ thay vì trả phản hồi.</summary>
    /// <remarks>
    /// Nhánh ngoại lệ của <c>SendAsync</c> trước đây không có test nào chạm tới, vì kịch
    /// bản chỉ biết trả <c>Task.FromResult</c>. Đó đúng là vùng chứa bộ lọc bắt ngoại lệ
    /// của <c>TransientRetryHandler</c> và của <c>ChatEndpoints</c> — tức là phần logic
    /// tinh vi nhất trong đường gọi ra ngoài lại là phần chưa từng được chạy trong test.
    /// </remarks>
    public GhiLaiHandler Nem(Exception loi, string? duongDanMongDoi = null)
    {
        ArgumentNullException.ThrowIfNull(loi);

        _kichBan.Enqueue((request, _) =>
        {
            KiemDuongDan(request, duongDanMongDoi);
            throw loi;
        });
        return this;
    }

    private static void KiemDuongDan(HttpRequestMessage request, string? duongDanMongDoi)
    {
        if (duongDanMongDoi is null)
        {
            return;
        }

        string thuc = request.RequestUri?.AbsolutePath ?? "(không có URI)";
        if (!thuc.Equals(duongDanMongDoi, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Kịch bản lệch pha: bước này chờ '{duongDanMongDoi}' nhưng nhận '{thuc}'.");
        }
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        DaGoi.Add(request);

        // Hết kịch bản thì 500 — im lặng trả 200 rỗng sẽ biến một test thiếu kịch bản
        // thành một test xanh vô nghĩa.
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> tiep =
            _kichBan.Count > 0
                ? _kichBan.Dequeue()
                : (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        return tiep(request, cancellationToken);
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
