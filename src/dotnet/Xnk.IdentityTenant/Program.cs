using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
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

// ─── Chống dò mật khẩu ───────────────────────────────────────────────────────────────
//
// `/token` là endpoint ẩn danh duy nhất của hệ thống, và mỗi lần gọi nó đốt 600.000 vòng
// PBKDF2 — con số đó chọn để mật khẩu đắt với người dò, nhưng nó đắt với CPU của chính
// mình y hệt. Chuỗi băm giả (xem TokenEndpoints._bamGia) còn bảo đảm email KHÔNG tồn tại
// cũng trả đúng cái giá ấy. Không có gì chặn thì một vòng lặp curl là vừa dò mật khẩu vừa
// làm cạn CPU của service phát token cho cả bảy service còn lại.
//
// Ba lớp, mỗi lớp bịt một hình dạng tấn công khác nhau:
//   ① theo IP        — một máy gõ thử nhiều lần
//   ② trần đồng thời — nhiều máy cùng lúc, nhằm vào CPU chứ không nhằm vào mật khẩu
//   ③ theo email     — nhiều máy, mỗi máy vài lần, cùng nhắm một tài khoản
//      (FailedLoginTracker, gọi trong TokenEndpoints)
//
// ⚠️ Lớp ① đếm theo RemoteIpAddress. Sau nginx/ALB mà không cấu hình ForwardedHeaders thì
// đó là IP của proxy, tức MỌI người dùng chung một ngăn. Local hiện chưa cấu hình cái đó,
// nên ở local lớp ① chặt hơn thực tế mong muốn còn trên AWS thì lỏng hơn — phải xử lý khi
// làm E1-04/05/06 (VPC/ALB). Lớp ② và ③ không phụ thuộc IP nên vẫn đúng ở cả hai nơi.
// Ngưỡng đọc từ cấu hình chứ không hằng cứng, để bộ test chứng minh được hàng rào này
// thật sự chặn: hằng cứng 10 thì test phải bắn 11 lời gọi HTTP thật, mỗi lời gọi một vòng
// PBKDF2, chỉ để thấy lời gọi thứ 11 bị từ chối. Hạ ngưỡng xuống 3 trong đúng một test là
// rẻ hơn và kiểm cùng một thứ.
//
// Việc này KHÔNG phải để tránh làm hỏng các test khác: xUnit dựng một host mới cho mỗi
// test method, nên mỗi test có bộ đếm riêng và không test nào hiện chạm trần.
int gioiHanTheoIp = builder.Configuration.GetValue("RateLimiting:TokenPermitLimit", 10);
int cuaSoGiay = builder.Configuration.GetValue("RateLimiting:TokenWindowSeconds", 60);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, huy) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan thu))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)thu.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        await context.HttpContext.Response.WriteAsync("Quá nhiều lần thử. Chờ rồi thử lại.", huy);
    };

    // ① Theo IP.
    options.AddPolicy(TokenEndpoints.ChinhSachGioiHan, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "khong-ro-ip",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = gioiHanTheoIp,
                Window = TimeSpan.FromSeconds(cuaSoGiay),
                // QueueLimit = 0: xếp hàng ở đây nghĩa là GIỮ kết nối của người dò lại rồi
                // vẫn phục vụ họ muộn hơn. Từ chối thẳng rẻ hơn cho cả hai phía.
                QueueLimit = 0,
            }));

    // ② Trần đồng thời, chỉ áp cho đường /token.
    //
    // Giới hạn theo IP không cứu được CPU khi lời gọi đến từ hàng nghìn địa chỉ. Trần này
    // nói: dù ai gọi, tại một thời điểm chỉ có tối đa ProcessorCount phép tính PBKDF2 chạy
    // song song. Hàng đợi ngắn để lúc quá tải người dùng thật nhận 429 nhanh thay vì treo.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        context.Request.Path.StartsWithSegments("/token", StringComparison.OrdinalIgnoreCase)
            ? RateLimitPartition.GetConcurrencyLimiter(
                "token",
                _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit = Math.Max(2, Environment.ProcessorCount),
                    QueueLimit = 20,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                })
            : RateLimitPartition.GetNoLimiter<string>("khac"));
});

// ③ Bộ đếm thất bại theo email.
//
// SizeLimit bắt buộc, không phải trang trí: khoá của bộ đếm là email do người gọi gửi lên,
// nên một vòng lặp với email ngẫu nhiên sẽ bơm bộ nhớ cho tới khi tiến trình chết — tức là
// biện pháp chống DoS tự trở thành đường DoS. 10.000 mục là thừa cho một prototype.
builder.Services.AddMemoryCache(options => options.SizeLimit = 10_000);
builder.Services.AddSingleton<FailedLoginTracker>();

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

// Đứng TRƯỚC xác thực: chặn ở đây thì lời gọi bị loại trước khi chạm database và trước
// khi ai đó phải trả giá 600.000 vòng PBKDF2. Đặt sau thì hàng rào vẫn tính đúng số lần
// nhưng công việc đắt tiền đã làm xong rồi mới bị chặn.
app.UseRateLimiter();

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
