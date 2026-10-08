using Microsoft.Extensions.Caching.Memory;
using Xnk.IdentityTenant.Security;

namespace Xnk.IdentityTenant.Tests.Security;

/// <summary>
/// Bộ đếm đăng nhập thất bại theo email.
/// </summary>
/// <remarks>
/// Test đơn vị, không chạm database — nên chúng chạy cả khi máy không có Docker, khác với
/// <c>TokenEndpointTests</c>. Đó là lý do phần logic đếm nằm trong một lớp riêng thay vì
/// viết thẳng vào endpoint.
/// </remarks>
public sealed class FailedLoginTrackerTests
{
    private static FailedLoginTracker Moi()
        => new(new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 }));

    [Fact]
    public void Chua_that_bai_lan_nao_thi_khong_bi_chan()
    {
        Assert.False(Moi().DangBiChan("ai-do@test.local"));
    }

    [Fact]
    public void Duoi_nguong_thi_chua_bi_chan()
    {
        FailedLoginTracker boDem = Moi();

        for (int i = 0; i < FailedLoginTracker.NguongThatBai - 1; i++)
        {
            boDem.GhiThatBai("ai-do@test.local");
        }

        Assert.False(boDem.DangBiChan("ai-do@test.local"));
    }

    [Fact]
    public void Cham_nguong_thi_bi_chan()
    {
        FailedLoginTracker boDem = Moi();

        for (int i = 0; i < FailedLoginTracker.NguongThatBai; i++)
        {
            boDem.GhiThatBai("ai-do@test.local");
        }

        Assert.True(boDem.DangBiChan("ai-do@test.local"));
    }

    [Fact]
    public void Chan_email_nay_khong_chan_email_khac()
    {
        // Bộ đếm dùng chung một ngăn cho mọi email sẽ biến một tài khoản bị dò thành một
        // sự cố ngừng dịch vụ cho cả tổ chức — tức là tặng cho người tấn công đúng thứ họ
        // muốn bằng chính biện pháp phòng thủ.
        FailedLoginTracker boDem = Moi();

        for (int i = 0; i < FailedLoginTracker.NguongThatBai; i++)
        {
            boDem.GhiThatBai("nan-nhan@test.local");
        }

        Assert.True(boDem.DangBiChan("nan-nhan@test.local"));
        Assert.False(boDem.DangBiChan("nguoi-khac@test.local"));
    }

    [Fact]
    public void Thanh_cong_thi_bo_dem_ve_khong()
    {
        FailedLoginTracker boDem = Moi();

        for (int i = 0; i < FailedLoginTracker.NguongThatBai - 1; i++)
        {
            boDem.GhiThatBai("ai-do@test.local");
        }

        boDem.XoaSauKhiThanhCong("ai-do@test.local");

        // Sau khi xoá, đếm lại từ đầu: bốn lần nữa vẫn chưa chạm ngưỡng.
        for (int i = 0; i < FailedLoginTracker.NguongThatBai - 1; i++)
        {
            boDem.GhiThatBai("ai-do@test.local");
        }

        Assert.False(boDem.DangBiChan("ai-do@test.local"));
    }

    [Fact]
    public void Bo_dem_khong_biet_email_co_that_hay_khong()
    {
        // ⚠️ Bất biến quan trọng nhất của lớp này, và cũng là thứ dễ bị "tối ưu" mất nhất.
        //
        // Nó không nhận User, không tra database, không có đường nào biết email có tồn tại
        // hay không — và đó là chủ đích. Chặn riêng cho tài khoản có thật thì chính thời
        // điểm bị chặn trở thành câu trả lời cho "email này có trong hệ thống không", dựng
        // lại đúng lỗ rò mà chuỗi băm giả ở TokenEndpoints sinh ra để bịt.
        FailedLoginTracker boDem = Moi();

        for (int i = 0; i < FailedLoginTracker.NguongThatBai; i++)
        {
            boDem.GhiThatBai("hoan-toan-bia@khong-ton-tai.local");
        }

        Assert.True(boDem.DangBiChan("hoan-toan-bia@khong-ton-tai.local"));
    }
}
