using System.Text.Json;
using Microsoft.JSInterop;

namespace Xnk.Web.Services;

/// <summary>
/// Giữ token của phiên đăng nhập hiện tại.
/// </summary>
/// <remarks>
/// <para>
/// Lưu ở <c>sessionStorage</c> chứ không phải <c>localStorage</c>: token hết hiệu lực khi
/// đóng tab. Với một hệ thống tra cứu dùng chung máy ở bãi container, để token sống qua
/// nhiều phiên trình duyệt là mời người tiếp theo dùng danh tính của người trước.
/// </para>
/// <para>
/// ⚠️ Đây vẫn là JWT trong bộ nhớ trình duyệt, tức là đọc được bởi mã JavaScript chạy cùng
/// trang. Cách kín hơn là cookie <c>HttpOnly</c> do một BFF phát — nhưng nó đòi một tầng
/// server-side mà prototype chưa có. Ghi ra đây để lựa chọn này là một quyết định nhìn thấy
/// được, không phải một thói quen chép từ ví dụ trên mạng.
/// </para>
/// </remarks>
/// <param name="js">Cầu nối JavaScript.</param>
public sealed class PhienDangNhap(IJSRuntime js)
{
    private const string _khoa = "xnk.token";

    private string? _token;
    private bool _daNap;

    /// <summary>Phát khi trạng thái đăng nhập đổi, để layout vẽ lại.</summary>
    public event Action? DaDoi;

    /// <summary>Token hiện tại, hoặc <c>null</c> nếu chưa đăng nhập.</summary>
    public string? Token => _token;

    /// <summary>Đã đăng nhập hay chưa.</summary>
    public bool DaDangNhap => !string.IsNullOrEmpty(_token);

    /// <summary>Đọc token đã lưu từ phiên trước của cùng tab.</summary>
    public async Task NapAsync()
    {
        if (_daNap)
        {
            return;
        }

        _daNap = true;
        _token = await js.InvokeAsync<string?>("sessionStorage.getItem", _khoa);
        DaDoi?.Invoke();
    }

    /// <summary>Lưu token sau khi đăng nhập thành công.</summary>
    public async Task DatAsync(string token)
    {
        _token = token;
        await js.InvokeVoidAsync("sessionStorage.setItem", _khoa, token);
        DaDoi?.Invoke();
    }

    /// <summary>Xoá token.</summary>
    public async Task XoaAsync()
    {
        _token = null;
        await js.InvokeVoidAsync("sessionStorage.removeItem", _khoa);
        DaDoi?.Invoke();
    }

    /// <summary>
    /// Đọc email từ claim của token, chỉ để hiển thị.
    /// </summary>
    /// <remarks>
    /// ⚠️ Giải mã ở client **không phải xác thực**. Chữ ký không được kiểm ở đây, và không
    /// cần: mọi quyết định về quyền đều do server đưa ra khi nó tự kiểm token. Dùng giá trị
    /// này cho bất cứ việc gì ngoài hiển thị là nhầm lẫn nguy hiểm.
    /// </remarks>
    public string? EmailHienThi()
    {
        if (string.IsNullOrEmpty(_token))
        {
            return null;
        }

        string[] phan = _token.Split('.');
        if (phan.Length != 3)
        {
            return null;
        }

        try
        {
            string payload = phan[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');

            using JsonDocument doc = JsonDocument.Parse(Convert.FromBase64String(payload));
            return doc.RootElement.TryGetProperty("email", out JsonElement email)
                ? email.GetString()
                : null;
        }
        catch (Exception loi) when (loi is FormatException or JsonException)
        {
            return null;
        }
    }
}
