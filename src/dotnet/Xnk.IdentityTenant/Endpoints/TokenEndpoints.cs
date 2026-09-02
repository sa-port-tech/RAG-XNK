using Microsoft.EntityFrameworkCore;
using Xnk.IdentityTenant.Contracts;
using Xnk.IdentityTenant.Data;
using Xnk.IdentityTenant.Security;

namespace Xnk.IdentityTenant.Endpoints;

/// <summary>
/// Endpoint phát token.
/// </summary>
public static class TokenEndpoints
{
    /// <summary>
    /// Chuỗi băm giả dùng khi không tìm thấy người dùng.
    /// </summary>
    /// <remarks>
    /// Không tìm thấy email mà trả 401 ngay lập tức thì thời gian phản hồi của "email không
    /// tồn tại" ngắn hơn hẳn "email tồn tại, sai mật khẩu" — đủ để dò xem một địa chỉ có
    /// trong hệ thống hay không, chỉ bằng cách bấm giờ. Kiểm mật khẩu với chuỗi này làm hai
    /// nhánh tốn thời gian tương đương.
    /// <para>
    /// Băm từ một mật khẩu ngẫu nhiên lúc khởi động: không ai — kể cả người đọc mã nguồn —
    /// biết mật khẩu khớp với nó.
    /// </para>
    /// </remarks>
    private static readonly string _bamGia = PasswordHasher.Bam(Guid.NewGuid().ToString("N"));

    /// <summary>Gắn nhóm endpoint xác thực vào ứng dụng.</summary>
    public static IEndpointRouteBuilder MapTokenEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/token", PhatToken)
            // Hiển nhiên nhưng phải khai: đây là đường vào để LẤY token, nên nó không thể
            // đòi có token. Thiếu dòng này thì middleware xác thực chặn mọi lần đăng nhập.
            .AllowAnonymous()
            .WithName("PhatToken")
            .WithSummary("Đổi email + mật khẩu lấy JWT")
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();

        return endpoints;
    }

    private static async Task<IResult> PhatToken(
        TokenRequest yeuCau,
        IdentityDbContext db,
        TokenIssuer nguoiPhat,
        ILogger<TokenRequest> log,
        CancellationToken huy)
    {
        if (string.IsNullOrWhiteSpace(yeuCau.Email) || string.IsNullOrWhiteSpace(yeuCau.Password))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = ["Email và mật khẩu đều bắt buộc."],
            });
        }

        string email = yeuCau.Email.Trim().ToLowerInvariant();

        User? nguoiDung = await db.Users
            .Include(u => u.Tenant)
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == email, huy);

        // Kiểm băm ở CẢ HAI nhánh — xem chú thích ở _bamGia.
        bool matKhauDung = PasswordHasher.KiemTra(
            yeuCau.Password,
            nguoiDung?.PasswordHash ?? _bamGia);

        if (nguoiDung is null || !matKhauDung || !nguoiDung.IsActive || nguoiDung.Tenant is null)
        {
            // Một thông báo duy nhất cho mọi lý do thất bại: email không tồn tại, sai mật
            // khẩu, tài khoản bị khoá. Phân biệt ra là tặng cho người dò một bộ lọc.
            //
            // Log thì ghi rõ hơn, vì log không đi ra ngoài — nhưng vẫn không ghi mật khẩu.
            log.LogInformation(
                "Đăng nhập thất bại cho {Email}: tồn tại={TonTai}, mật khẩu đúng={MatKhauDung}, đang hoạt động={HoatDong}",
                email,
                nguoiDung is not null,
                matKhauDung,
                nguoiDung?.IsActive);

            return TypedResults.Problem(
                title: "Đăng nhập thất bại",
                detail: "Email hoặc mật khẩu không đúng.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        TokenIssuer.KetQua ketQua = nguoiPhat.Phat(nguoiDung, nguoiDung.Tenant);

        return TypedResults.Ok(new TokenResponse(ketQua.AccessToken, "Bearer", ketQua.ExpiresIn));
    }
}
