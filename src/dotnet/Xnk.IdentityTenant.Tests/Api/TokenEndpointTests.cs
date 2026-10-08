using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xnk.IdentityTenant.Data;
using Xnk.IdentityTenant.Security;
using Xnk.Shared.Tenancy;

namespace Xnk.IdentityTenant.Tests.Api;

/// <summary>
/// Endpoint phát token, gọi qua HTTP thật.
/// </summary>
[Collection(IdentityPostgresCollection.Name)]
public sealed class TokenEndpointTests(IdentityPostgresFixture postgres) : IAsyncLifetime
{
    private const string _khoaKy = "khoa-ky-danh-rieng-cho-test-dai-32-ky-tu";
    private const string _issuer = "https://identity.test.local";
    private const string _audience = "xnk-api-test";
    private const string _matKhau = "mat-khau-test-2026";

    private readonly string _emailHoatDong = $"hoat-dong-{Guid.NewGuid():N}@test.local";
    private readonly string _emailBiKhoa = $"bi-khoa-{Guid.NewGuid():N}@test.local";

    private WebApplicationFactory<Program> _factory = null!;
    private Guid _tenantId;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _factory = new IdentityApiFactory(postgres.ConnectionString);

        await using IdentityDbContext db = postgres.CreateContext();

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Slug = $"tenant-{Guid.NewGuid():N}",
            Name = "Tenant dùng cho test",
            Type = "b2b_khach_hang",
        };
        _tenantId = tenant.Id;

        // Số vòng thấp: bộ test này kiểm luồng đăng nhập, không kiểm sức chống dò. Băm với
        // 600.000 vòng cho từng test làm bộ test chậm đi hàng giây mà không chứng minh
        // thêm điều gì — PasswordHasherTests mới là chỗ giữ tham số thật.
        string bam = PasswordHasher.Bam(_matKhau, soVong: 1_000);

        db.Tenants.Add(tenant);
        db.Users.AddRange(
            new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Email = _emailHoatDong,
                PasswordHash = bam,
                Role = "admin",
                IsActive = true,
            },
            new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Email = _emailBiKhoa,
                PasswordHash = bam,
                Role = "user",
                IsActive = false,
            });

        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Dang_nhap_dung_thi_tra_token_mang_du_ba_claim_tenant()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage phanHoi = await Dang(client, _emailHoatDong, _matKhau);
        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);

        PhanHoiToken? than = await phanHoi.Content.ReadFromJsonAsync<PhanHoiToken>();
        Assert.NotNull(than);
        Assert.Equal("Bearer", than.TokenType);
        Assert.True(than.ExpiresIn > 0);

        // Token phải qua được ĐÚNG bộ tham số mà sáu service kia dùng để kiểm tra. Kiểm
        // bằng cách tự giải mã chuỗi là tự chấm bài mình: nó bỏ qua chính phần dễ sai nhất.
        var handler = new JsonWebTokenHandler();
        TokenValidationResult ketQua = await handler.ValidateTokenAsync(than.AccessToken, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_khoaKy)),
            ValidateLifetime = true,
        });

        Assert.True(ketQua.IsValid);
        Assert.Equal(_tenantId.ToString(), ketQua.Claims[TenantClaims.TenantId].ToString());
        Assert.Equal("b2b_khach_hang", ketQua.Claims[TenantClaims.TenantType].ToString());
        Assert.Equal("admin", ketQua.Claims[TenantClaims.Role].ToString());
    }

    [Fact]
    public async Task Token_bi_ky_bang_khoa_khac_thi_khong_hop_le()
    {
        using HttpClient client = _factory.CreateClient();
        HttpResponseMessage phanHoi = await Dang(client, _emailHoatDong, _matKhau);
        PhanHoiToken? than = await phanHoi.Content.ReadFromJsonAsync<PhanHoiToken>();
        Assert.NotNull(than);

        var handler = new JsonWebTokenHandler();
        TokenValidationResult ketQua = await handler.ValidateTokenAsync(than.AccessToken, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("mot-khoa-hoan-toan-khac-cung-dai-32-ky-tu")),
            ValidateLifetime = true,
        });

        Assert.False(ketQua.IsValid);
    }

    [Theory]
    [InlineData("mat-khau-sai")]
    [InlineData("")]
    public async Task Sai_mat_khau_thi_401(string matKhau)
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage phanHoi = await Dang(client, _emailHoatDong, matKhau);

        Assert.True(phanHoi.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Email_khong_ton_tai_thi_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage phanHoi = await Dang(client, $"khong-co-{Guid.NewGuid():N}@test.local", _matKhau);

        Assert.Equal(HttpStatusCode.Unauthorized, phanHoi.StatusCode);
    }

    [Fact]
    public async Task Tai_khoan_bi_khoa_thi_401_du_mat_khau_dung()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage phanHoi = await Dang(client, _emailBiKhoa, _matKhau);

        Assert.Equal(HttpStatusCode.Unauthorized, phanHoi.StatusCode);
    }

    [Fact]
    public async Task Email_khong_ton_tai_va_sai_mat_khau_tra_cung_mot_thong_bao()
    {
        // Phân biệt hai lý do là tặng cho người dò một bộ lọc để biết email nào có thật.
        //
        // So `title` + `detail` + mã trạng thái, KHÔNG so nguyên thân phản hồi:
        // ProblemDetails mang thêm `traceId` khác nhau ở mỗi request — đó là thứ có ích khi
        // tra log, và nó không nói gì về việc tài khoản có tồn tại hay không.
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage saiMatKhau = await Dang(client, _emailHoatDong, "mat-khau-sai");
        HttpResponseMessage khongCoEmail = await Dang(
            client, $"khong-co-{Guid.NewGuid():N}@test.local", _matKhau);

        Assert.Equal(saiMatKhau.StatusCode, khongCoEmail.StatusCode);

        ChiTietLoi? a = await saiMatKhau.Content.ReadFromJsonAsync<ChiTietLoi>();
        ChiTietLoi? b = await khongCoEmail.Content.ReadFromJsonAsync<ChiTietLoi>();

        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.Equal(a.Title, b.Title);
        Assert.Equal(a.Detail, b.Detail);
        Assert.Equal(a.Status, b.Status);
    }

    [Fact]
    public async Task Duong_dan_co_tien_to_la_duong_duoc_phuc_vu()
    {
        // ADR-013: ALB không cắt tiền tố, nên ứng dụng nhận nguyên `/identity-tenant/...`.
        //
        // ⚠️ `UsePathBase` chỉ CẮT tiền tố khi nó có mặt, và route vẫn khai là `/token`,
        // nên đường trần cũng chạy. Ba service Python gắn prefix thẳng vào router nên ở đó
        // đường trần trả 404. Cả hai đều đúng sau ALB — ghi ra để không ai sửa nhầm.
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage coTienTo = await Dang(client, _emailHoatDong, _matKhau);
        Assert.Equal(HttpStatusCode.OK, coTienTo.StatusCode);

        HttpResponseMessage khongTienTo = await client.PostAsJsonAsync(
            new Uri("/token", UriKind.Relative),
            new { email = _emailHoatDong, password = _matKhau });
        Assert.Equal(HttpStatusCode.OK, khongTienTo.StatusCode);
    }

    [Fact]
    public async Task Vuot_gioi_han_theo_IP_thi_429()
    {
        // `/token` là endpoint ẩn danh duy nhất của hệ thống và mỗi lời gọi đốt 600.000
        // vòng PBKDF2 — kể cả khi email không tồn tại, vì chuỗi băm giả cố tình làm hai
        // nhánh tốn thời gian như nhau. Không có hàng rào thì một vòng lặp curl vừa dò mật
        // khẩu vừa làm cạn CPU của service phát token cho cả bảy service còn lại.
        await using var factory = new IdentityApiFactory(postgres.ConnectionString, gioiHanTheoIp: 3);
        using HttpClient client = factory.CreateClient();

        for (int i = 0; i < 3; i++)
        {
            HttpResponseMessage trongHan = await Dang(client, _emailHoatDong, _matKhau);
            Assert.Equal(HttpStatusCode.OK, trongHan.StatusCode);
        }

        HttpResponseMessage vuot = await Dang(client, _emailHoatDong, _matKhau);

        Assert.Equal(HttpStatusCode.TooManyRequests, vuot.StatusCode);
    }

    [Fact]
    public async Task Sai_lien_tiep_qua_nguong_thi_email_bi_chan_429()
    {
        // Giới hạn theo IP không chặn được kiểu tấn công ngược lại: nhiều máy, mỗi máy thử
        // vài lần, cùng nhắm một tài khoản — hình dạng của một đợt credential stuffing bằng
        // danh sách mật khẩu rò rỉ. Bộ đếm theo email bịt vế đó.
        //
        // Giới hạn IP để rộng ở đây: test này kiểm bộ đếm theo email, không kiểm hàng rào IP.
        using HttpClient client = _factory.CreateClient();

        for (int i = 0; i < FailedLoginTracker.NguongThatBai; i++)
        {
            HttpResponseMessage that = await Dang(client, _emailHoatDong, "mat-khau-sai");
            Assert.Equal(HttpStatusCode.Unauthorized, that.StatusCode);
        }

        // Lần thứ sáu bị chặn — và bị chặn TRƯỚC khi chạm database, nên đúng mật khẩu cũng
        // không qua. Đây là điều phân biệt "chặn tạm thời" với "sai mật khẩu".
        HttpResponseMessage biChan = await Dang(client, _emailHoatDong, _matKhau);

        Assert.Equal(HttpStatusCode.TooManyRequests, biChan.StatusCode);
    }

    [Fact]
    public async Task Email_khong_ton_tai_cung_bi_chan_nhu_email_co_that()
    {
        // ⚠️ Đây là vế dễ làm hỏng nhất khi ai đó "tối ưu" bộ đếm cho chỉ đếm tài khoản có
        // thật. Chặn riêng cho email tồn tại thì chính thời điểm bị chặn trả lời câu hỏi
        // "email này có trong hệ thống không" — dựng lại đúng lỗ rò mà chuỗi băm giả sinh
        // ra để bịt, chỉ khác là lần này rò qua mã trạng thái thay vì qua thời gian.
        string emailMa = $"khong-co-{Guid.NewGuid():N}@test.local";
        using HttpClient client = _factory.CreateClient();

        for (int i = 0; i < FailedLoginTracker.NguongThatBai; i++)
        {
            HttpResponseMessage that = await Dang(client, emailMa, "mat-khau-sai");
            Assert.Equal(HttpStatusCode.Unauthorized, that.StatusCode);
        }

        HttpResponseMessage biChan = await Dang(client, emailMa, "mat-khau-sai");

        Assert.Equal(HttpStatusCode.TooManyRequests, biChan.StatusCode);
    }

    [Fact]
    public async Task Dang_nhap_thanh_cong_thi_xoa_bo_dem()
    {
        // Bốn lần gõ nhầm rồi nhớ ra mật khẩu là chuyện thường ngày, không phải một đợt tấn
        // công đang diễn ra dở. Không xoá bộ đếm thì người dùng thật bị chặn ở lần gõ nhầm
        // thứ năm của cả tuần, và không hiểu vì sao.
        using HttpClient client = _factory.CreateClient();

        for (int i = 0; i < FailedLoginTracker.NguongThatBai - 1; i++)
        {
            await Dang(client, _emailHoatDong, "mat-khau-sai");
        }

        HttpResponseMessage dung = await Dang(client, _emailHoatDong, _matKhau);
        Assert.Equal(HttpStatusCode.OK, dung.StatusCode);

        // Sau khi xoá, bốn lần sai nữa vẫn chưa chạm ngưỡng.
        for (int i = 0; i < FailedLoginTracker.NguongThatBai - 1; i++)
        {
            HttpResponseMessage that = await Dang(client, _emailHoatDong, "mat-khau-sai");
            Assert.Equal(HttpStatusCode.Unauthorized, that.StatusCode);
        }
    }

    private static Task<HttpResponseMessage> Dang(HttpClient client, string email, string matKhau)
        => client.PostAsJsonAsync(
            new Uri("/identity-tenant/token", UriKind.Relative),
            new { email, password = matKhau });

    /// <summary>Phần của ProblemDetails mà test cần so sánh.</summary>
    private sealed record ChiTietLoi(string? Title, string? Detail, int? Status);

    /// <remarks>
    /// Tên trường theo RFC 6749 §5.1 — snake_case, khớp <c>TokenResponse</c> của service.
    /// Thiếu <c>JsonPropertyName</c> ở đây thì phản hồi vẫn đúng nhưng test đọc ra
    /// <c>null</c>, và người đọc sẽ đi tìm lỗi ở phía service.
    /// </remarks>
    private sealed record PhanHoiToken(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("token_type")] string TokenType,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresIn);

    private sealed class IdentityApiFactory(string connectionString, int gioiHanTheoIp = 1_000)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Production để appsettings.Development.json không được nạp — nếu nạp, chuỗi
            // kết nối của máy dev sẽ đè lên chuỗi test và bộ test âm thầm chạy trên
            // database thật của người đang code.
            builder.UseEnvironment(Environments.Production);

            builder.UseSetting("ConnectionStrings:Identity", connectionString);
            builder.UseSetting("Jwt:Issuer", _issuer);
            builder.UseSetting("Jwt:Audience", _audience);
            builder.UseSetting("Jwt:SigningKey", _khoaKy);

            // Nới rộng cho các test KHÔNG kiểm giới hạn tần suất, để chúng không đỏ vì một
            // lý do chẳng liên quan gì tới thứ chúng kiểm. Test nào kiểm chính hàng rào đó
            // thì tự truyền một con số nhỏ.
            builder.UseSetting(
                "RateLimiting:TokenPermitLimit",
                gioiHanTheoIp.ToString(CultureInfo.InvariantCulture));
        }
    }
}
