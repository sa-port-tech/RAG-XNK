using System.Globalization;
using Npgsql;

namespace Xnk.TestSupport;

/// <summary>
/// Đọc địa chỉ PostgreSQL dùng cho test từ môi trường, chấp nhận cả hai định dạng.
/// </summary>
/// <remarks>
/// <para>
/// File này được <b>link</b> vào cả <c>Xnk.Corpus.Tests</c> lẫn
/// <c>Xnk.IdentityTenant.Tests</c> thay vì chép hai bản. Hai fixture vẫn tách riêng — chúng
/// sở hữu hai lược đồ khác nhau và migration chạy độc lập, đó là lý do chính đáng — nhưng
/// phần <i>đọc biến môi trường và quy đổi định dạng</i> thì không có lý do gì để tồn tại
/// hai bản.
/// </para>
/// <para>
/// <b>Một tên biến, một định dạng.</b> Trước đây .NET đọc <c>XNK_TEST_CONNECTION</c> (chuỗi
/// kiểu ADO.NET <c>Host=…;Port=…</c>) còn Python đọc <c>XNK_TEST_DATABASE_URL</c> (DSN
/// <c>postgresql://…</c>) — cùng một database, hai tên, hai định dạng, và
/// <c>.env.example</c> chỉ nói về một trong hai. Một người chạy bộ test Python ở máy mình
/// không có cách nào biết biến kia tồn tại.
/// </para>
/// <para>
/// Nay cả hai phía dùng <c>XNK_TEST_DATABASE_URL</c> ở dạng DSN. Dạng DSN được chọn vì nó
/// là dạng mà <c>psql</c>, <c>db/apply-migrations.sh</c> và ba service Python đều đã dùng —
/// đổi phía .NET là đổi một chỗ, đổi phía kia là đổi bốn.
/// </para>
/// </remarks>
internal static class ChuoiKetNoiTest
{
    /// <summary>Tên biến môi trường, dùng chung cho cả .NET lẫn Python.</summary>
    public const string TenBienMoiTruong = "XNK_TEST_DATABASE_URL";

    /// <summary>Ảnh container, khớp đúng ảnh trong CI và docker-compose.</summary>
    public const string AnhPostgres = "pgvector/pgvector:pg16";

    /// <summary>
    /// Giá trị đã quy đổi sang chuỗi kết nối Npgsql, hoặc <c>null</c> nếu biến chưa đặt.
    /// </summary>
    public static string? TuMoiTruong()
    {
        string? tho = Environment.GetEnvironmentVariable(TenBienMoiTruong);

        return string.IsNullOrWhiteSpace(tho) ? null : QuyDoi(tho);
    }

    /// <summary>Quy đổi DSN <c>postgresql://…</c> sang chuỗi kết nối Npgsql.</summary>
    /// <remarks>
    /// Giá trị đã ở dạng khoá-giá trị thì trả nguyên: người chạy test trên máy mình có thể
    /// dán thẳng chuỗi mà Npgsql quen thuộc, và bắt họ đổi sang DSN chỉ để chiều một quy
    /// ước là thêm ma sát mà không đổi lấy gì.
    /// </remarks>
    public static string QuyDoi(string gia_tri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gia_tri);

        if (!Uri.TryCreate(gia_tri, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            return gia_tri;
        }

        string[] thongTin = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.TrimStart('/'),
        };

        if (thongTin.Length > 0 && thongTin[0].Length > 0)
        {
            builder.Username = Uri.UnescapeDataString(thongTin[0]);
        }

        if (thongTin.Length > 1)
        {
            builder.Password = Uri.UnescapeDataString(thongTin[1]);
        }

        return builder.ConnectionString.ToString(CultureInfo.InvariantCulture);
    }
}
