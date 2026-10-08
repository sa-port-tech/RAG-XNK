using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xnk.Shared.Tenancy;

namespace Xnk.Shared.Hosting;

/// <summary>
/// Những thứ mọi service .NET đều phải có, và phải có giống nhau.
/// </summary>
public static class ServiceDefaultsExtensions
{
    /// <summary>Tag đánh dấu health check thuộc nhóm readiness.</summary>
    public const string ReadinessTag = "ready";

    /// <summary>
    /// Đăng ký ngữ cảnh tenant và phần xử lý lỗi chuẩn.
    /// </summary>
    public static IServiceCollection AddXnkServiceDefaults(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();

        // Trả lỗi theo RFC 7807 ở mọi service. Client Blazor xử lý đúng một dạng lỗi
        // thay vì đoán theo từng endpoint.
        services.AddProblemDetails();

        return services;
    }

    /// <summary>
    /// Gắn hai endpoint healthcheck dưới tiền tố đường dẫn của service.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>/health/live</c> — tiến trình còn sống. KHÔNG chạm phụ thuộc ngoài: liveness mà
    /// gọi database thì một sự cố database sẽ khiến ECS giết và khởi động lại toàn bộ
    /// service, biến sự cố phụ thuộc thành sự cố lan rộng.
    /// </para>
    /// <para>
    /// <c>/health/ready</c> — sẵn sàng nhận lưu lượng, chỉ chạy các check mang tag
    /// <see cref="ReadinessTag"/>. Đây là đường dẫn mà <c>smoke_test.sh</c> gọi sau mỗi
    /// lần deploy, theo <c>health_path</c> trong <c>.github/services.json</c>.
    /// </para>
    /// <para>
    /// Cả hai để <c>AllowAnonymous</c>: ALB gọi chúng và ALB không có token.
    /// </para>
    /// </remarks>
    public static IEndpointRouteBuilder MapXnkHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            // Không check nào chạy — chỉ cần tiến trình trả lời được là đủ.
            Predicate = _ => false,
        }).AllowAnonymous();

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadinessTag),
            ResponseWriter = GhiKetQuaAsync,
        }).AllowAnonymous();

        return endpoints;
    }

    /// <summary>
    /// Ghi kết quả readiness dưới dạng JSON, liệt kê <b>từng</b> check.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bộ ghi mặc định chỉ in đúng một từ: <c>Healthy</c>. Với một service không đăng ký
    /// check nào, từ đó vẫn được in ra — và người đọc không có cách nào phân biệt "đã kiểm
    /// ba thứ, cả ba đều tốt" với "chưa kiểm gì cả". Chat từng ở đúng trạng thái thứ hai,
    /// trong khi <c>smoke_test.sh</c> đọc mã 200 của nó như bằng chứng deploy thành công.
    /// </para>
    /// <para>
    /// Liệt kê từng check làm cho danh sách rỗng trở nên <b>nhìn thấy được</b>. Nó cũng là
    /// chỗ trạng thái <c>Degraded</c> nói được điều gì đó: HTTP vẫn 200 (không rút service
    /// khỏi vòng phục vụ), nhưng thân phản hồi nêu đích danh phụ thuộc nào đang hỏng.
    /// </para>
    /// </remarks>
    private static Task GhiKetQuaAsync(HttpContext context, HealthReport baoCao)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var than = new
        {
            status = baoCao.Status.ToString(),
            totalDurationMs = (long)baoCao.TotalDuration.TotalMilliseconds,
            checks = baoCao.Entries.Select(muc => new
            {
                name = muc.Key,
                status = muc.Value.Status.ToString(),
                description = muc.Value.Description,
            }),
        };

        return context.Response.WriteAsJsonAsync(than);
    }
}
