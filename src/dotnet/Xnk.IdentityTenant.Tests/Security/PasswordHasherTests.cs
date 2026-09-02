using Xnk.IdentityTenant.Security;

namespace Xnk.IdentityTenant.Tests.Security;

/// <summary>
/// Băm và kiểm mật khẩu.
/// </summary>
public sealed class PasswordHasherTests
{
    [Fact]
    public void Bam_roi_kiem_lai_thi_dung()
    {
        // Số vòng nhỏ cho nhanh: test này kiểm LOGIC, không kiểm sức chống dò.
        string luu = PasswordHasher.Bam("mat-khau-dung", soVong: 1_000);

        Assert.True(PasswordHasher.KiemTra("mat-khau-dung", luu));
    }

    [Fact]
    public void Sai_mat_khau_thi_khong_qua()
    {
        string luu = PasswordHasher.Bam("mat-khau-dung", soVong: 1_000);

        Assert.False(PasswordHasher.KiemTra("mat-khau-sai", luu));
    }

    [Fact]
    public void Hai_lan_bam_cung_mot_mat_khau_cho_hai_chuoi_khac_nhau()
    {
        // Muối ngẫu nhiên. Nếu hai chuỗi giống nhau nghĩa là muối đã thành cố định ở đâu
        // đó, và khi ấy một bảng tra dựng sẵn phá được toàn bộ bảng người dùng cùng lúc.
        string a = PasswordHasher.Bam("cung-mot-mat-khau", soVong: 1_000);
        string b = PasswordHasher.Bam("cung-mot-mat-khau", soVong: 1_000);

        Assert.NotEqual(a, b);
        Assert.True(PasswordHasher.KiemTra("cung-mot-mat-khau", a));
        Assert.True(PasswordHasher.KiemTra("cung-mot-mat-khau", b));
    }

    [Theory]
    [InlineData("")]
    [InlineData("khong-phai-dinh-dang-nao-ca")]
    [InlineData("bcrypt$1000$muoi$bam")]                 // sai thuật toán
    [InlineData("pbkdf2_sha256$khong-phai-so$bXVvaQ==$YmFt")]  // số vòng hỏng
    [InlineData("pbkdf2_sha256$1000$khong-phai-base64!$YmFt")] // muối hỏng
    [InlineData("pbkdf2_sha256$1000$bXVvaQ==")]          // thiếu phần
    public void Chuoi_luu_hong_thi_tra_false_chu_khong_nem_ngoai_le(string chuoiLuu)
    {
        // Dữ liệu hỏng phải dẫn tới "đăng nhập thất bại", không dẫn tới 500. Một endpoint
        // đăng nhập trả 500 cho đúng một tài khoản là đã tiết lộ điều gì đó về tài khoản ấy.
        Assert.False(PasswordHasher.KiemTra("bat-ky", chuoiLuu));
    }

    [Fact]
    public void Doc_duoc_chuoi_bam_do_script_seed_Python_sinh_ra()
    {
        // ⚠️ HỢP ĐỒNG LIÊN NGÔN NGỮ — đừng "dọn dẹp" test này.
        //
        // Chuỗi dưới đây do tools/local/sinh_seed.py sinh bằng hashlib.pbkdf2_hmac, và nó
        // là thứ nằm trong db/seed/0003_identity_tenants_users.sql. Nếu định dạng chuỗi
        // hoặc tham số băm ở một phía đổi mà phía kia không đổi theo, bốn tài khoản mẫu
        // ngừng đăng nhập được ở mọi máy dev — với triệu chứng giống hệt "sai mật khẩu",
        // tức là chỗ cuối cùng người ta nghĩ tới sẽ là định dạng băm.
        //
        // Đây là tài khoản an.nguyen@noibo.vn trong seed.
        const string tuSeed =
            "pbkdf2_sha256$600000$fWGzkSRv2rDbLc44flzaAA==$Wr+/XGLf8005unV3fVN+lzUuBYvpNxegovM6m1Buo7I=";

        Assert.True(PasswordHasher.KiemTra("matkhau-local-2026", tuSeed));
        Assert.False(PasswordHasher.KiemTra("mat-khau-khac", tuSeed));
    }
}
