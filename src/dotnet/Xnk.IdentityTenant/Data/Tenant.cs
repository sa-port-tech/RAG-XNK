namespace Xnk.IdentityTenant.Data;

/// <summary>
/// Một tổ chức dùng hệ thống.
/// </summary>
/// <remarks>
/// docs/00 §12 chia tenant làm ba loại và mô hình đó giữ nguyên khi production chuyển sang
/// SSO. Bảng này là nơi <c>tenant_id</c> trong mọi JWT bắt nguồn — và cũng là nơi
/// <c>TenantId</c> của <c>corpus.documents</c> trỏ tới về mặt nghiệp vụ.
/// <para>
/// Không có khoá ngoại nào giữa hai bảng đó: chúng thuộc hai schema của hai service khác
/// nhau, và docs/00 §4.4 cấm ràng buộc chéo schema. Ràng buộc nằm ở tầng nghiệp vụ, đổi
/// lại quyền tách cơ sở dữ liệu về sau mà không phải gỡ khoá ngoại nào.
/// </para>
/// </remarks>
public sealed class Tenant
{
    /// <summary>Khoá chính. Chính là giá trị đi vào claim <c>tenant_id</c>.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Định danh đọc được, ổn định, dùng trong seed và trong tài liệu.
    /// </summary>
    /// <remarks>
    /// Có slug thì khoá chính của dữ liệu seed sinh được bằng UUIDv5 từ chuỗi này, nên nó
    /// giống nhau ở mọi máy dev mà không ai phải giữ một bảng ánh xạ GUID.
    /// </remarks>
    public required string Slug { get; set; }

    /// <summary>Tên hiển thị.</summary>
    public required string Name { get; set; }

    /// <summary>
    /// Loại tenant: <c>noi_bo</c> | <c>b2b_khach_hang</c> | <c>dao_tao</c>.
    /// </summary>
    /// <remarks>
    /// Giá trị đi thẳng vào claim <c>tenant_type</c>, nên tập giá trị phải khớp đúng phần
    /// mô tả ở <see cref="Xnk.Shared.Tenancy.TenantClaims.TenantType"/>. Ràng buộc CHECK
    /// nằm trong lược đồ chứ không chỉ trong mã: một giá trị lạ lọt vào đây sẽ đi ra ngoài
    /// theo token và các service khác chỉ đơn giản là không hiểu nó.
    /// </remarks>
    public required string Type { get; set; }

    /// <summary>Thời điểm tạo, theo UTC.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Người dùng thuộc tenant này.</summary>
    public ICollection<User> Users { get; } = [];
}
