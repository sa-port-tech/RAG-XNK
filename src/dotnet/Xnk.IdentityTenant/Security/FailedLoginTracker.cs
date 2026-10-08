using Microsoft.Extensions.Caching.Memory;

namespace Xnk.IdentityTenant.Security;

/// <summary>
/// Đếm số lần đăng nhập thất bại theo email và chặn tạm thời khi vượt ngưỡng.
/// </summary>
/// <remarks>
/// <para>
/// Giới hạn theo IP (xem <c>Program.cs</c>) chặn một máy gõ thử nhiều lần. Nó <b>không</b>
/// chặn được kiểu ngược lại: nhiều máy, mỗi máy thử vài lần, cùng nhắm một tài khoản —
/// đúng hình dạng của một đợt credential stuffing bằng danh sách mật khẩu rò rỉ. Bộ đếm
/// này bịt vế đó.
/// </para>
/// <para>
/// <b>Đếm theo email đã gửi lên, không đếm theo email có thật.</b> Điểm này quan trọng:
/// nếu chỉ đếm cho tài khoản tồn tại thì thời điểm bị chặn tự nó tiết lộ email nào có
/// trong hệ thống — đúng thứ mà chuỗi băm giả ở <c>TokenEndpoints</c> dựng ra để giấu.
/// </para>
/// <para>
/// <b>Bộ nhớ trong tiến trình, không dùng chung giữa các replica.</b> Chạy N bản thì
/// ngưỡng thực tế là N lần ngưỡng khai báo. Chấp nhận được cho prototype một replica;
/// khi lên ECS nhiều task thì chuyển sang Redis hoặc bảng trong <c>identity</c> — và
/// <b>phải chuyển</b>, đừng để lại một biện pháp bảo vệ mà hiệu lực loãng dần mỗi lần
/// scale out mà không ai nhận ra.
/// </para>
/// <para>
/// Đây không phải khoá tài khoản. Không có cột nào trong database bị đổi, và cửa sổ chặn
/// tự hết hạn — vì một cơ chế khoá tài khoản mà ai cũng kích hoạt được từ bên ngoài là
/// một cách khoá người dùng thật ra khỏi hệ thống của họ.
/// </para>
/// </remarks>
public sealed class FailedLoginTracker(IMemoryCache boNho)
{
    /// <summary>Số lần thất bại liên tiếp trước khi bị chặn.</summary>
    public const int NguongThatBai = 5;

    /// <summary>Thời gian chặn sau khi vượt ngưỡng.</summary>
    public static readonly TimeSpan ThoiGianChan = TimeSpan.FromMinutes(15);

    private readonly IMemoryCache _boNho = boNho;

    private static string Khoa(string email) => $"dang-nhap-that-bai:{email}";

    /// <summary>Email này có đang bị chặn không.</summary>
    public bool DangBiChan(string email)
        => _boNho.TryGetValue(Khoa(email), out int soLan) && soLan >= NguongThatBai;

    /// <summary>Ghi một lần thất bại.</summary>
    /// <remarks>
    /// Cửa sổ trượt: mỗi lần thất bại đẩy hạn hết hiệu lực về sau, nên năm lần rải đều
    /// trong một giờ vẫn bị chặn. Đếm trong cửa sổ cố định thì kẻ dò chỉ cần chờ hết
    /// cửa sổ rồi thử tiếp năm lần nữa, mãi mãi.
    /// </remarks>
    public void GhiThatBai(string email)
    {
        string khoa = Khoa(email);
        int soLan = _boNho.TryGetValue(khoa, out int hienTai) ? hienTai + 1 : 1;

        _boNho.Set(khoa, soLan, new MemoryCacheEntryOptions
        {
            SlidingExpiration = ThoiGianChan,
            Size = 1,
        });
    }

    /// <summary>Xoá bộ đếm sau một lần đăng nhập thành công.</summary>
    public void XoaSauKhiThanhCong(string email) => _boNho.Remove(Khoa(email));
}
