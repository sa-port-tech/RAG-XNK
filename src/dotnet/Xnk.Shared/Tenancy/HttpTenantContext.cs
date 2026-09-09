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

            // null ở đây nghĩa là "request này không có người dùng đã xác thực" — ví dụ
            // healthcheck, hoặc một endpoint AllowAnonymous. Global query filter đọc null
            // là "chỉ thấy tài liệu dùng chung", đúng ngữ nghĩa cần cho những chỗ đó.
            //
            // Nó KHÔNG còn nghĩa là "đã xác thực nhưng thiếu claim tenant". Trường hợp ấy
            // bị chặn sớm hơn, ở JwtAuthenticationExtensions.CoDuClaimBatBuoc: token thiếu
            // `tenant_id` hoặc mang `tenant_id` không phải GUID đều bị từ chối bằng 401.
            //
            // Vì sao phải chặn ở đó chứ không ở đây: chỗ này không có đường nào báo lỗi.
            // Trả null là câu trả lời hợp lệ với người gọi, nên một token hỏng sẽ cho ra
            // 200 kèm danh sách ngắn hơn người dùng tưởng — an toàn nhưng im lặng, và cái
            // im lặng đó mới là vấn đề. Vẫn giữ TryParse để phòng thân: một ngày nào đó có
            // người đăng ký ITenantContext ở nơi chưa qua middleware xác thực.
            return Guid.TryParse(raw, out Guid tenantId) ? tenantId : null;
        }
    }
}
