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

    /// <summary>Tên chính sách giới hạn tần suất áp cho <c>/token</c>.</summary>
    public const string ChinhSachGioiHan = "dang-nhap";

    /// <summary>Gắn nhóm endpoint xác thực vào ứng dụng.</summary>
    public static IEndpointRouteBuilder MapTokenEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/token", PhatToken)
            // Hiển nhiên nhưng phải khai: đây là đường vào để LẤY token, nên nó không thể
            // đòi có token. Thiếu dòng này thì middleware xác thực chặn mọi lần đăng nhập.
            .AllowAnonymous()
            // ⚠️ `AllowAnonymous` mà không có dòng dưới là công thức của endpoint đắt nhất
            // hệ thống mở toang cho mọi người — xem khối chú thích ở Program.cs.
            .RequireRateLimiting(ChinhSachGioiHan)
            .WithName("PhatToken")
            .WithSummary("Đổi email + mật khẩu lấy JWT")
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesValidationProblem();

        return endpoints;
    }

    private static async Task<IResult> PhatToken(
        TokenRequest yeuCau,
        IdentityDbContext db,
        TokenIssuer nguoiPhat,
        FailedLoginTracker boDem,
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

        // Chặn TRƯỚC khi chạm database và trước khi trả giá 600.000 vòng PBKDF2.
        //
        // Trả 429 cho mọi email vượt ngưỡng, dù email đó có thật hay không — bộ đếm không
        // biết và cố tình không cần biết. Chặn riêng cho tài khoản có thật thì chính thời
        // điểm bị chặn trở thành câu trả lời cho "email này có trong hệ thống không", tức
        // là dựng lại đúng lỗ rò mà chuỗi băm giả ở trên sinh ra để bịt.
        if (boDem.DangBiChan(email))
        {
            log.LogWarning(
                "Chặn tạm thời: một email vượt {Nguong} lần thất bại liên tiếp trong {Phut} phút.",
                FailedLoginTracker.NguongThatBai,
                FailedLoginTracker.ThoiGianChan.TotalMinutes);

            return TypedResults.Problem(
                title: "Quá nhiều lần thử",
                detail: "Tài khoản này đang bị tạm chặn đăng nhập. Thử lại sau ít phút.",
                statusCode: StatusCodes.Status429TooManyRequests);
        }

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
            // ⚠️ KHÔNG ghi email vào log. Email là thông tin cá nhân, và log của một
            // endpoint đăng nhập là nơi nó tích tụ nhanh nhất — mỗi lần gõ nhầm mật khẩu
            // là một dòng, giữ lại hàng tháng, đọc được bởi bất kỳ ai có quyền xem log
            // (CodeQL: exposure of private information).
            //
            // Ba cờ dưới đây đủ để người trực biết chuyện gì xảy ra. Cần lần ra đúng tài
            // khoản nào thì tra bằng định danh khác, không phải bằng cách đổ PII vào log.
            log.LogInformation(
                "Đăng nhập thất bại: tồn tại={TonTai}, mật khẩu đúng={MatKhauDung}, đang hoạt động={HoatDong}",
                nguoiDung is not null,
                matKhauDung,
                nguoiDung?.IsActive);

            boDem.GhiThatBai(email);

            return TypedResults.Problem(
                title: "Đăng nhập thất bại",
                detail: "Email hoặc mật khẩu không đúng.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        // Đăng nhập được thì xoá bộ đếm: năm lần gõ nhầm rồi nhớ ra mật khẩu là chuyện
        // thường ngày, không phải một đợt tấn công đang diễn ra dở.
        boDem.XoaSauKhiThanhCong(email);

        TokenIssuer.KetQua ketQua = nguoiPhat.Phat(nguoiDung, nguoiDung.Tenant);

        return TypedResults.Ok(new TokenResponse(ketQua.AccessToken, "Bearer", ketQua.ExpiresIn));
    }
}
