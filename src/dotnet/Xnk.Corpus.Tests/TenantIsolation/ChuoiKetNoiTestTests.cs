using Npgsql;
using Xnk.TestSupport;

namespace Xnk.Corpus.Tests.TenantIsolation;

/// <summary>
/// Quy đổi DSN sang chuỗi kết nối Npgsql.
/// </summary>
/// <remarks>
/// Logic này đứng giữa CI và database: sai nó thì mọi test chạm database đỏ với thông báo
/// "không nối được", và chỗ cuối cùng người ta nghĩ tới sẽ là một hàm chuyển chuỗi.
/// </remarks>
public sealed class ChuoiKetNoiTestTests
{
    [Theory]
    [InlineData("postgresql://nguoi:matkhau@may-chu:5433/csdl")]
    [InlineData("postgres://nguoi:matkhau@may-chu:5433/csdl")]
    public void Quy_doi_DSN_day_du(string dsn)
    {
        var builder = new NpgsqlConnectionStringBuilder(ChuoiKetNoiTest.QuyDoi(dsn));

        Assert.Equal("may-chu", builder.Host);
        Assert.Equal(5433, builder.Port);
        Assert.Equal("csdl", builder.Database);
        Assert.Equal("nguoi", builder.Username);
        Assert.Equal("matkhau", builder.Password);
    }

    [Fact]
    public void Thieu_cong_thi_ve_5432()
    {
        var builder = new NpgsqlConnectionStringBuilder(
            ChuoiKetNoiTest.QuyDoi("postgresql://nguoi:matkhau@may-chu/csdl"));

        Assert.Equal(5432, builder.Port);
    }

    [Fact]
    public void Mat_khau_ma_hoa_URL_duoc_giai_ma()
    {
        // Mật khẩu sinh ngẫu nhiên thường chứa `@`, `/`, `:` — đúng những ký tự phải mã hoá
        // trong URL. Không giải mã thì chuỗi kết nối mang mật khẩu SAI, và triệu chứng là
        // "xác thực thất bại" chứ không phải "chuỗi kết nối hỏng".
        var builder = new NpgsqlConnectionStringBuilder(
            ChuoiKetNoiTest.QuyDoi("postgresql://nguoi:m%40t%2Fkhau@may-chu:5432/csdl"));

        Assert.Equal("m@t/khau", builder.Password);
    }

    [Fact]
    public void Chuoi_kieu_Npgsql_thi_giu_nguyen()
    {
        // Ai quen dạng khoá-giá trị vẫn dán thẳng được. Bắt họ đổi sang DSN chỉ để chiều
        // một quy ước là thêm ma sát mà không đổi lấy gì.
        const string nguyenBan = "Host=localhost;Port=5432;Database=xnk_test;Username=postgres";

        Assert.Equal(nguyenBan, ChuoiKetNoiTest.QuyDoi(nguyenBan));
    }
}
