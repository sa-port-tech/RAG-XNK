using Xnk.Chat.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xnk.Chat.Clients;
using Xnk.Chat.Endpoints;
using Xnk.Chat.Http;
using Xnk.Shared.Authentication;
using Xnk.Shared.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddXnkServiceDefaults();
builder.Services.AddXnkJwtAuthentication(builder.Configuration);

builder.Services.AddOptions<DownstreamOptions>()
    .Bind(builder.Configuration.GetSection(DownstreamOptions.SectionName))
    .ValidateDataAnnotations()
    // Địa chỉ service phía sau thiếu hoặc sai định dạng thì service chết ngay lúc khởi
    // động, kèm tên trường. Không có nó thì lỗi chỉ lộ ra ở câu hỏi đầu tiên của người
    // dùng — tức là sau khi deploy đã báo thành công.
    .ValidateOnStart();

builder.Services.AddTransient<ForwardAuthorizationHandler>();
builder.Services.AddTransient<TransientRetryHandler>();

builder.Services.AddHttpClient(TenClient.Retrieval, (sp, client) =>
    {
        DownstreamOptions o = sp.GetRequiredService<IOptions<DownstreamOptions>>().Value;
        client.BaseAddress = new Uri(o.RetrievalBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(o.RetrievalTimeoutSeconds);
    })
    .AddHttpMessageHandler<ForwardAuthorizationHandler>()
    // Chỉ retrieval mới có handler thử lại: nó được gọi bằng GET, không tác dụng phụ.
    .AddHttpMessageHandler<TransientRetryHandler>();

builder.Services.AddHttpClient(TenClient.Generation, (sp, client) =>
    {
        DownstreamOptions o = sp.GetRequiredService<IOptions<DownstreamOptions>>().Value;
        client.BaseAddress = new Uri(o.GenerationBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(o.GenerationTimeoutSeconds);
    })
    .AddHttpMessageHandler<ForwardAuthorizationHandler>();
// KHÔNG gắn TransientRetryHandler ở đây: lời gọi là POST, và một lần sinh chữ mất hàng
// chục giây — thử lại chỉ nhân đôi thời gian người dùng phải chờ.

builder.Services.AddScoped<RetrievalClient>();
builder.Services.AddScoped<GenerationClient>();

// Readiness KHÔNG gọi sang retrieval/generation.
//
// Nghe có vẻ ngược, nên ghi rõ: readiness trả lời "tiến trình này có nhận được lưu lượng
// không", còn phụ thuộc chết là chuyện của phụ thuộc đó. Nếu chat báo not-ready khi
// generation chết, ALB sẽ rút chat khỏi vòng phục vụ — và người dùng nhận lỗi mạng thay vì
// một thông báo 502 nói rõ chuyện gì đang xảy ra. Tệ hơn: hai service cùng kiểm chéo nhau
// là công thức để một sự cố nhỏ hạ cả cụm.
//
// Chat vẫn phục vụ được khi phụ thuộc chết — nó trả 502 kèm lý do, và đó là hành vi đúng.
//
// Nhưng "không gate readiness vào phụ thuộc" KHÔNG có nghĩa là readiness không kiểm gì.
// Bản trước gọi AddHealthChecks() rỗng, nên /chat/health/ready trả 200 vô điều kiện — nó
// là liveness với một URL dài hơn, trong khi smoke_test.sh đọc đúng mã đó như bằng chứng
// deploy thành công.
//
// Hai check dưới đây trả tối đa Degraded, và Degraded vẫn cho ra HTTP 200: ALB không rút
// chat khỏi vòng phục vụ, còn thân phản hồi thì nêu đích danh phụ thuộc nào đang hỏng.
// Báo cáo, không phải cổng. Xem DownstreamHealthCheck.
//
// Đăng ký qua HealthCheckRegistration với factory nhận IServiceProvider, KHÔNG dựng sẵn
// instance: dựng sẵn buộc phải gọi BuildServiceProvider() ngay giữa lúc đăng ký, tức là
// tạo ra một container thứ hai với vòng đời riêng — mọi singleton sau đó tồn tại hai bản.
builder.Services.AddHealthChecks()
    .Add(new HealthCheckRegistration(
        "retrieval",
        sp => new DownstreamHealthCheck(
            sp.GetRequiredService<IHttpClientFactory>(),
            TenClient.Retrieval,
            "/retrieval/health/ready"),
        failureStatus: HealthStatus.Degraded,
        tags: [ServiceDefaultsExtensions.ReadinessTag]))
    .Add(new HealthCheckRegistration(
        "generation",
        sp => new DownstreamHealthCheck(
            sp.GetRequiredService<IHttpClientFactory>(),
            TenClient.Generation,
            "/generation/health/ready"),
        failureStatus: HealthStatus.Degraded,
        tags: [ServiceDefaultsExtensions.ReadinessTag]));

builder.Services.AddOpenApi();

WebApplication app = builder.Build();

// ⚠️ PHẢI đứng trước mọi middleware định tuyến — ALB không cắt tiền tố (ADR-013).
app.UsePathBase("/chat");

app.UseAuthentication();
app.UseAuthorization();

app.MapXnkHealthEndpoints();
app.MapChatEndpoints();

app.MapOpenApi();

await app.RunAsync();

/// <summary>
/// Lộ ra cho project test.
/// </summary>
public partial class Program;
