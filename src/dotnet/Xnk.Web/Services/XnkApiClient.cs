using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Xnk.Web.Services;

/// <summary>Kết quả một lời gọi API, hoặc lỗi đã được diễn giải.</summary>
/// <typeparam name="T">Kiểu dữ liệu trả về khi thành công.</typeparam>
/// <param name="GiaTri">Dữ liệu, hoặc <c>null</c> khi lỗi.</param>
/// <param name="Loi">Thông báo lỗi đã sẵn sàng để hiển thị, hoặc <c>null</c> khi thành công.</param>
public readonly record struct KetQua<T>(T? GiaTri, string? Loi)
{
    /// <summary>Thành công hay không.</summary>
    public bool ThanhCong => Loi is null;
}

/// <summary>Thân lỗi theo RFC 7807 mà mọi service .NET đều trả.</summary>
/// <remarks>
/// Mọi service bật <c>AddProblemDetails()</c> trong <c>AddXnkServiceDefaults</c>, nên client
/// chỉ phải xử lý **một** dạng lỗi thay vì đoán theo từng endpoint.
/// </remarks>
public sealed record ProblemDetails(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("detail")] string? Detail,
    [property: JsonPropertyName("status")] int? Status);

/// <summary>Một văn bản trong danh sách.</summary>
public sealed record VanBan(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("documentNumber")] string DocumentNumber,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("isShared")] bool IsShared);

/// <summary>Một trang kết quả.</summary>
public sealed record TrangVanBan(
    [property: JsonPropertyName("items")] IReadOnlyList<VanBan> Items,
    [property: JsonPropertyName("totalCount")] int TotalCount);

/// <summary>Phản hồi phát token.</summary>
public sealed record PhanHoiToken(
    [property: JsonPropertyName("access_token")] string AccessToken);

/// <summary>
/// Gọi API của hệ thống.
/// </summary>
/// <remarks>
/// Mọi đường dẫn là **tương đối**: nginx phục vụ cả trang này lẫn API trên cùng một origin
/// (xem `tools/local/render_nginx.py`), nên không có CORS và không có địa chỉ nào phải cấu
/// hình theo môi trường. Trên AWS, CloudFront đóng vai tương tự trước ALB.
/// </remarks>
/// <param name="http">Client đã gắn <see cref="TokenHandler"/>.</param>
public sealed class XnkApiClient(HttpClient http)
{
    /// <summary>Đổi email + mật khẩu lấy token.</summary>
    public async Task<KetQua<string>> DangNhapAsync(string email, string matKhau, CancellationToken huy = default)
    {
        try
        {
            HttpResponseMessage phanHoi = await http.PostAsJsonAsync(
                "identity-tenant/token", new { email, password = matKhau }, huy);

            if (phanHoi.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Server cố ý trả cùng một thông báo cho mọi lý do thất bại — giữ nguyên
                // điều đó ở client. Đoán thêm "email không tồn tại" là phá lớp phòng thủ
                // mà phía kia dựng lên.
                return new(null, "Email hoặc mật khẩu không đúng.");
            }

            if (!phanHoi.IsSuccessStatusCode)
            {
                return new(null, await DienGiaiLoiAsync(phanHoi, huy));
            }

            PhanHoiToken? than = await phanHoi.Content.ReadFromJsonAsync<PhanHoiToken>(huy);
            return than is null
                ? new(null, "Máy chủ trả về phản hồi rỗng.")
                : new(than.AccessToken, null);
        }
        catch (HttpRequestException loi)
        {
            return new(null, $"Không gọi được máy chủ: {loi.Message}");
        }
    }

    /// <summary>Lấy danh sách văn bản mà tenant hiện tại được xem.</summary>
    public async Task<KetQua<TrangVanBan>> LayVanBanAsync(CancellationToken huy = default)
    {
        try
        {
            HttpResponseMessage phanHoi = await http.GetAsync(
                new Uri("corpus/documents?pageSize=100", UriKind.Relative), huy);

            if (phanHoi.StatusCode == HttpStatusCode.Unauthorized)
            {
                return new(null, "Phiên đăng nhập đã hết hạn. Hãy đăng nhập lại.");
            }

            if (!phanHoi.IsSuccessStatusCode)
            {
                return new(null, await DienGiaiLoiAsync(phanHoi, huy));
            }

            return new(await phanHoi.Content.ReadFromJsonAsync<TrangVanBan>(huy), null);
        }
        catch (HttpRequestException loi)
        {
            return new(null, $"Không gọi được máy chủ: {loi.Message}");
        }
    }

    private static async Task<string> DienGiaiLoiAsync(HttpResponseMessage phanHoi, CancellationToken huy)
    {
        try
        {
            ProblemDetails? chiTiet = await phanHoi.Content.ReadFromJsonAsync<ProblemDetails>(huy);
            if (chiTiet?.Title is not null)
            {
                return chiTiet.Detail is null ? chiTiet.Title : $"{chiTiet.Title}: {chiTiet.Detail}";
            }
        }
        catch (Exception loi) when (loi is System.Text.Json.JsonException or NotSupportedException)
        {
            // Không phải ProblemDetails thì rơi xuống thông báo theo mã trạng thái.
        }

        return $"Máy chủ trả về lỗi {(int)phanHoi.StatusCode}.";
    }
}
