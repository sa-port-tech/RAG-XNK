using System.ComponentModel.DataAnnotations;
using System.Text;
using Xnk.Shared.Authentication;

namespace Xnk.IdentityTenant.Tests.Security;

/// <summary>
/// Ràng buộc trên khoá ký JWT.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="JwtOptions"/> nằm ở <c>Xnk.Shared</c> nhưng test lại đặt ở đây, vì
/// <c>Xnk.Shared</c> chưa có bộ test riêng và identity-tenant là service <b>phát</b> token
/// — khoá của nó là khoá của cả bảy service (ADR-014). Khi nào <c>Xnk.Shared.Tests</c> ra
/// đời thì dời sang đó.
/// </para>
/// <para>
/// Kiểm qua <see cref="Validator"/> chứ không gọi thẳng <c>Validate</c>: đó đúng là đường
/// mà <c>ValidateDataAnnotations().ValidateOnStart()</c> đi, nên test này hỏng đúng lúc
/// service khởi động sẽ hỏng.
/// </para>
/// </remarks>
public sealed class JwtOptionsTests
{
    private static IReadOnlyList<ValidationResult> Kiem(string khoaKy)
    {
        JwtOptions tuyChon = new()
        {
            Issuer = "https://identity.xnk.local",
            Audience = "xnk-api",
            SigningKey = khoaKy,
        };

        List<ValidationResult> ketQua = [];
        Validator.TryValidateObject(tuyChon, new ValidationContext(tuyChon), ketQua, validateAllProperties: true);
        return ketQua;
    }

    [Fact]
    public void Khoa_du_dai_thi_hop_le()
    {
        Assert.Empty(Kiem(new string('k', 32)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("qua-ngan")]
    [InlineData("CHAY-sinh-khoa.sh")] // đúng giá trị mẫu trong .env.example
    [InlineData("31-ky-tu-van-con-thieu-mot-byt")]
    public void Khoa_ngan_hon_32_byte_thi_bi_chan(string khoaKy)
    {
        Assert.NotEmpty(Kiem(khoaKy));
    }

    [Fact]
    public void Gia_tri_mau_trong_env_example_KHONG_khoi_dong_duoc()
    {
        // Đây là điều kiện khiến bước `bash tools/local/sinh-khoa.sh` là BẮT BUỘC chứ không
        // phải một lời khuyên trong README. Nếu ai đó "sửa cho tiện" bằng cách kéo dài giá
        // trị mẫu trong .env.example cho qua ngưỡng, test này đỏ — và đó là ý đồ.
        const string giaTriMau = "CHAY-sinh-khoa.sh";

        Assert.True(Encoding.UTF8.GetByteCount(giaTriMau) < JwtOptions.SoByteToiThieu);
        Assert.NotEmpty(Kiem(giaTriMau));
    }

    [Fact]
    public void Khoa_da_tung_nam_trong_git_thi_bi_chan_du_rat_dai()
    {
        // ⚠️ HỒI QUY. Chuỗi này dài 51 ký tự và qua mọi phép đo độ dài — nó bị chặn KHÔNG
        // phải vì yếu mà vì đã được commit: ai đọc lịch sử repo cũng ký được token hợp lệ
        // cho cả bảy service, do prototype dùng chung một khoá đối xứng (ADR-014).
        //
        // Đo ngày 08/09/2026: .env trên máy dev mang y nguyên giá trị này, dù .env.example
        // có dặn "chép rồi sửa theo máy mình". Lời dặn không đổi được hành vi khi bỏ qua nó
        // không gây ra hậu quả nào; chỉ có việc khởi động thất bại mới đổi được.
        const string khoaCu = "khoa-ky-chi-dung-cho-may-local-khong-phai-bi-mat-32";

        Assert.True(khoaCu.Length > JwtOptions.SoByteToiThieu);
        Assert.NotEmpty(Kiem(khoaCu));
    }

    [Fact]
    public void Khoa_it_ky_tu_nhung_du_byte_thi_hop_le()
    {
        // Vì sao đo BYTE chứ không đo ký tự: HMAC-SHA256 cần 32 byte khoá, và chữ tiếng
        // Việt có dấu chiếm 2-3 byte mỗi ký tự trong UTF-8. Ràng buộc cũ đếm ký tự nên
        // từ chối một khoá 20 ký tự dài 46 byte — đủ mạnh theo đúng thứ thuật toán cần.
        const string khoaCoDau = "khoá-ký-đủ-mạnh-nhưng-ít-ký-tự";

        Assert.True(khoaCoDau.Length < JwtOptions.SoByteToiThieu);
        Assert.True(Encoding.UTF8.GetByteCount(khoaCoDau) >= JwtOptions.SoByteToiThieu);
        Assert.Empty(Kiem(khoaCoDau));
    }
}
