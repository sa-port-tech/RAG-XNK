namespace Xnk.Shared.Tenancy;

/// <summary>
/// Ngữ cảnh tenant cố định, dùng ngoài đường đi của một HTTP request.
/// </summary>
/// <param name="tenantId">Tenant cần giả lập, hoặc <c>null</c> cho ngữ cảnh dùng chung.</param>
/// <remarks>
/// Hai chỗ dùng: test cách ly tenant, và các tiến trình nền (External Task Worker, công
/// cụ migration) vốn không có <c>HttpContext</c>.
/// <para>
/// ⚠️ Không đăng ký lớp này trong DI của một service đang phục vụ HTTP. Làm vậy là gắn
/// cứng mọi request vào một tenant — và đó chính là kịch bản rò rỉ chéo mà cả bộ máy
/// này sinh ra để ngăn.
/// </para>
/// </remarks>
public sealed class StaticTenantContext(Guid? tenantId) : ITenantContext
{
    /// <inheritdoc />
    public Guid? TenantId { get; } = tenantId;
}
