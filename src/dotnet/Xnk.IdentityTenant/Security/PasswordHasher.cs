using System.Security.Cryptography;
using System.Text;

namespace Xnk.IdentityTenant.Security;

/// <summary>
/// Băm và kiểm mật khẩu bằng PBKDF2-HMAC-SHA256.
/// </summary>
/// <remarks>
/// <para>
/// <b>Định dạng lưu:</b> <c>pbkdf2_sha256$&lt;số vòng&gt;$&lt;muối base64&gt;$&lt;băm base64&gt;</c>
/// </para>
/// <para>
/// Chuỗi tự mô tả — mang theo thuật toán, số vòng lặp và muối của chính nó. Nhờ vậy nâng
/// số vòng về sau không cần migration dữ liệu: bản ghi cũ vẫn kiểm được bằng tham số cũ,
/// và có thể băm lại lặng lẽ ở lần đăng nhập kế tiếp. Lưu số vòng trong một cột riêng thì
/// mọi thay đổi tham số đều thành một thay đổi lược đồ.
/// </para>
/// <para>
/// Định dạng này cũng là <b>hợp đồng liên ngôn ngữ</b>: script seed ở
/// <c>tools/local/sinh_seed.py</c> sinh ra đúng chuỗi như vậy bằng
/// <c>hashlib.pbkdf2_hmac</c>. Đó là lý do định dạng phải tầm thường và không phụ thuộc
/// vào cách .NET sắp xếp byte của riêng nó.
/// </para>
/// <para>
/// PBKDF2 chứ không phải Argon2id — vốn là lựa chọn tốt hơn — vì PBKDF2 nằm sẵn trong thư
/// viện chuẩn của cả .NET lẫn Python, không thêm phụ thuộc nào cho một prototype. Khi
/// production chuyển sang SSO (docs/00 §12), toàn bộ lớp này biến mất cùng với bảng mật
/// khẩu, nên đầu tư thêm vào đây là đầu tư vào thứ sắp bị bỏ.
/// </para>
/// </remarks>
public static class PasswordHasher
{
    /// <summary>Nhãn thuật toán, cũng là tiền tố của chuỗi lưu.</summary>
    public const string ThuatToan = "pbkdf2_sha256";

    /// <summary>
    /// Số vòng lặp cho bản ghi mới. Theo khuyến nghị OWASP cho PBKDF2-HMAC-SHA256.
    /// </summary>
    public const int SoVongMacDinh = 600_000;

    private const int _doDaiMuoi = 16;
    private const int _doDaiBam = 32;

    /// <summary>Băm một mật khẩu thành chuỗi tự mô tả.</summary>
    /// <param name="matKhau">Mật khẩu dạng chữ thường người dùng nhập.</param>
    /// <param name="soVong">Số vòng lặp; để mặc định trừ khi đang tái hiện một bản ghi cũ.</param>
    public static string Bam(string matKhau, int soVong = SoVongMacDinh)
    {
        ArgumentException.ThrowIfNullOrEmpty(matKhau);
        ArgumentOutOfRangeException.ThrowIfLessThan(soVong, 1);

        byte[] muoi = RandomNumberGenerator.GetBytes(_doDaiMuoi);
        byte[] bam = DanXuat(matKhau, muoi, soVong);

        return $"{ThuatToan}${soVong}${Convert.ToBase64String(muoi)}${Convert.ToBase64String(bam)}";
    }

    /// <summary>
    /// Kiểm mật khẩu với chuỗi băm đã lưu.
    /// </summary>
    /// <remarks>
    /// Chuỗi lưu hỏng định dạng thì trả <c>false</c> chứ không ném ngoại lệ: dữ liệu hỏng
    /// phải dẫn tới "đăng nhập thất bại", không dẫn tới 500 — một endpoint đăng nhập trả
    /// 500 cho đúng một tài khoản là đã tiết lộ điều gì đó về tài khoản ấy.
    /// </remarks>
    public static bool KiemTra(string matKhau, string chuoiLuu)
    {
        if (string.IsNullOrEmpty(matKhau) || string.IsNullOrEmpty(chuoiLuu))
        {
            return false;
        }

        string[] phan = chuoiLuu.Split('$');
        if (phan.Length != 4 || phan[0] != ThuatToan)
        {
            return false;
        }

        if (!int.TryParse(phan[1], out int soVong) || soVong < 1)
        {
            return false;
        }

        byte[] muoi;
        byte[] bamMongDoi;
        try
        {
            muoi = Convert.FromBase64String(phan[2]);
            bamMongDoi = Convert.FromBase64String(phan[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] bamThuc = DanXuat(matKhau, muoi, soVong, bamMongDoi.Length);

        // So sánh thời gian cố định: so sánh thường thoát sớm ở byte đầu tiên khác nhau,
        // và chênh lệch thời gian đó đo được qua mạng.
        return CryptographicOperations.FixedTimeEquals(bamThuc, bamMongDoi);
    }

    private static byte[] DanXuat(string matKhau, byte[] muoi, int soVong, int doDai = _doDaiBam)
        => Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(matKhau),
            muoi,
            soVong,
            HashAlgorithmName.SHA256,
            doDai);
}
