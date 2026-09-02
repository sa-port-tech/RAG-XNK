using Microsoft.Extensions.Options;
using Xnk.Shared.Hosting;
using Xnk.WorkflowWorker.Camunda;
using Xnk.WorkflowWorker.Handlers;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddXnkServiceDefaults();

// KHÔNG gọi AddXnkJwtAuthentication.
//
// Worker không có endpoint nào phục vụ người dùng: hai đường duy nhất nó mở là
// /health/live và /health/ready, và cả hai đều AllowAnonymous vì ALB gọi chúng mà ALB
// không có token. Đăng ký xác thực ở đây sẽ bắt service phải có khoá JWT trong cấu hình
// để khởi động được — một ràng buộc không phục vụ điều gì.

builder.Services.AddOptions<CamundaOptions>()
    .Bind(builder.Configuration.GetSection(CamundaOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<CamundaClient>((sp, client) =>
{
    CamundaOptions o = sp.GetRequiredService<IOptions<CamundaOptions>>().Value;

    // Dấu gạch chéo cuối là BẮT BUỘC: không có nó, Uri tương đối
    // "external-task/fetchAndLock" sẽ thay thế đoạn cuối của BaseAddress thay vì nối vào,
    // và mọi lời gọi đi lạc sang /external-task thay vì /engine-rest/external-task.
    client.BaseAddress = new Uri(o.BaseUrl.TrimEnd('/') + "/");

    // Dài hơn asyncResponseTimeout: fetchAndLock là long polling, engine cố ý giữ kết nối
    // lại tới khi có task. Đặt ngắn hơn thì client tự huỷ đúng lúc engine đang chờ hộ.
    client.Timeout = TimeSpan.FromMilliseconds(o.AsyncResponseTimeoutMs + 15_000);
});

builder.Services.AddSingleton<IExternalTaskHandler, NotifyExpertHandler>();
builder.Services.AddHostedService<ExternalTaskWorker>();

builder.Services.AddHealthChecks()
    .AddCheck<CamundaHealthCheck>("camunda", tags: [ServiceDefaultsExtensions.ReadinessTag]);

WebApplication app = builder.Build();

// ⚠️ PHẢI đứng trước mọi middleware định tuyến — ALB không cắt tiền tố (ADR-013).
app.UsePathBase("/workflow-worker");

app.MapXnkHealthEndpoints();

await app.RunAsync();

/// <summary>
/// Lộ ra cho project test.
/// </summary>
public partial class Program;
