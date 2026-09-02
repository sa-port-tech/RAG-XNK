using Microsoft.EntityFrameworkCore;
using Xnk.Corpus.Data;
using Xnk.Corpus.Endpoints;
using Xnk.Shared.Authentication;
using Xnk.Shared.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddXnkServiceDefaults();
builder.Services.AddXnkJwtAuthentication(builder.Configuration);

builder.Services.AddDbContext<CorpusDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Corpus"),
        npgsql => npgsql.MigrationsHistoryTable(
            "__ef_migrations_history", CorpusDbContext.SchemaName)));

// Readiness kiểm tra được database — đây là phụ thuộc mà thiếu nó service không phục vụ
// được request nào. Liveness thì không chạm tới, xem ServiceDefaultsExtensions.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CorpusDbContext>("corpus-db", tags: [ServiceDefaultsExtensions.ReadinessTag]);

builder.Services.AddOpenApi();

WebApplication app = builder.Build();

// ⚠️ PHẢI đứng trước mọi middleware định tuyến.
//
// ALB định tuyến theo tiền tố `/corpus/*` nhưng KHÔNG cắt tiền tố trước khi chuyển tiếp,
// nên ứng dụng nhận nguyên `/corpus/health/ready`. UsePathBase cắt tiền tố ra và đặt vào
// PathBase, nhờ đó route khai báo là `/health/ready` vẫn khớp — ở cả sau ALB lẫn khi
// chạy trực tiếp lúc dev. Xem ADR-0005.
//
// Tiền tố lấy từ trường `name` của service trong .github/services.json; đổi một bên mà
// quên bên kia thì smoke test sau deploy sẽ đỏ.
app.UsePathBase("/corpus");

app.UseAuthentication();
app.UseAuthorization();

app.MapXnkHealthEndpoints();
app.MapDocumentsEndpoints();

// Tài liệu OpenAPI sinh từ mã (ADR-0003) tại /corpus/openapi/v1.json.
app.MapOpenApi();

await app.RunAsync();

/// <summary>
/// Lộ ra cho project test.
/// </summary>
/// <remarks>
/// Top-level statement sinh ra một lớp <c>Program</c> internal. Khai báo partial công
/// khai ở đây để <c>WebApplicationFactory&lt;Program&gt;</c> dùng được trong test tích
/// hợp về sau, mà không phải mở toàn bộ assembly bằng InternalsVisibleTo.
/// </remarks>
public partial class Program;
