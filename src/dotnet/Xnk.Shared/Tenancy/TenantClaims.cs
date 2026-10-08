namespace Xnk.Shared.Tenancy;

/// <summary>
/// Tên claim dùng chung cho mọi service.
/// </summary>
/// <remarks>
/// Đặt ở thư viện chung chứ không ở từng service là có lý do: nếu identity-tenant phát
/// token với claim <c>tenant_id</c> còn corpus-service lại đọc <c>tenantId</c>, kết quả
/// không phải lỗi biên dịch mà là một service coi mọi request như không có tenant. Với
/// ngữ nghĩa ở <see cref="ITenantContext.TenantId"/>, hậu quả là người dùng không thấy
/// dữ liệu riêng của mình — khó chịu, nhưng an toàn, và lộ ra ngay.
/// </remarks>
public static class TenantClaims
{
    /// <summary>Định danh tenant, dạng GUID.</summary>
    public const string TenantId = "tenant_id";

    /// <summary>Loại tenant: <c>noi_bo</c> | <c>b2b_khach_hang</c> | <c>dao_tao</c> (docs/00 §12).</summary>
    public const string TenantType = "tenant_type";

    /// <summary>Vai trò trong tenant: <c>admin</c> | <c>user</c> | <c>viewer</c>.</summary>
    public const string Role = "role";
}
