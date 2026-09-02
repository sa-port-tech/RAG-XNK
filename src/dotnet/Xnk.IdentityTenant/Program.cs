using Microsoft.EntityFrameworkCore;
using Xnk.IdentityTenant.Data;
using Xnk.IdentityTenant.Endpoints;
using Xnk.IdentityTenant.Security;
using Xnk.Shared.Authentication;
using Xnk.Shared.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddXnkServiceDefaults();

// Service này PHÁT token, nhưng vẫn đăng ký cả phần KIỂM TRA. Hai lý do:
//   · API quản trị sắp tới (RBAC, quota) đòi token như mọi service khác;
//   · AddXnkJwtAuthentication bật ValidateOnStart cho JwtOptions, nên thiếu hoặc quá ngắn
//     khoá ký thì service chết ngay lúc khởi động — thay vì phát ra token không ai xác
//     thực nổi và chỉ phát hiện ở service thứ hai.
builder.Services.AddXnkJwtAuthentication(builder.Configuration);

builder.Services.AddOptions<TokenIssuingOptions>()
    .Bind(builder.Configuration.GetSection(TokenIssuingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<TokenIssuer>();

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Identity"),
        npgsql => npgsql.MigrationsHistoryTable(
            "__ef_migrations_history", IdentityDbContext.SchemaName)));

// Readiness chạm database: thiếu nó thì service này không phát nổi token nào. Liveness
// thì không chạm — xem ServiceDefaultsExtensions.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<IdentityDbContext>("identity-db", tags: [ServiceDefaultsExtensions.ReadinessTag]);

builder.Services.AddOpenApi();

WebApplication app = builder.Build();

// ⚠️ PHẢI đứng trước mọi middleware định tuyến — ALB không cắt tiền tố, xem ADR-013 và
// chú thích tương ứng trong Xnk.Corpus/Program.cs. Tiền tố lấy từ trường `name` của
// service trong .github/services.json.
app.UsePathBase("/identity-tenant");

app.UseAuthentication();
app.UseAuthorization();

app.MapXnkHealthEndpoints();
app.MapTokenEndpoints();

app.MapOpenApi();

await app.RunAsync();

/// <summary>
/// Lộ ra cho project test.
/// </summary>
/// <remarks>
/// Top-level statement sinh ra một lớp <c>Program</c> internal. Khai báo partial công khai
/// ở đây để <c>WebApplicationFactory&lt;Program&gt;</c> dùng được trong test tích hợp.
/// </remarks>
public partial class Program;
