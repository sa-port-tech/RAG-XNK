using Microsoft.AspNetCore.Http;

namespace Xnk.Shared.Tenancy;

/// <summary>
/// Lấy tenant từ claim của người dùng đã xác thực trong request hiện tại.
/// </summary>
/// <param name="httpContextAccessor">Truy cập request đang xử lý.</param>
/// <remarks>
/// Đọc lại claim ở mỗi lần truy cập thuộc tính thay vì lưu vào biến lúc khởi tạo: đối
/// tượng này đăng ký theo vòng đời scoped, nhưng nếu ai đó vô tình đăng ký nó thành
/// singleton thì việc đọc lại vẫn cho ra tenant của request hiện tại thay vì tenant của
/// request đầu tiên từng chạm tới nó. Chi phí là một lần duyệt danh sách claim ngắn.
/// </remarks>
public sealed class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    /// <inheritdoc />
    public Guid? TenantId
    {
        get
        {
            string? raw = _httpContextAccessor.HttpContext?.User.FindFirst(TenantClaims.TenantId)?.Value;

            // Claim thiếu hoặc không phải GUID đều cho ra null — tức là "chỉ thấy phần
            // dùng chung". Không ném exception ở đây: một token méo phải bị chặn ở tầng
            // xác thực, và nếu nó lọt qua được thì mất quyền xem vẫn tốt hơn là 500.
            return Guid.TryParse(raw, out Guid tenantId) ? tenantId : null;
        }
    }
}
