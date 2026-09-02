namespace Xnk.IdentityTenant.Data;

/// <summary>
/// Một người dùng, luôn thuộc đúng một tenant.
/// </summary>
public sealed class User
{
    /// <summary>Khoá chính. Đi vào claim <c>sub</c> của token.</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant sở hữu người dùng này.</summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Email đăng nhập, **duy nhất trên toàn hệ thống**.
    /// </summary>
    /// <remarks>
    /// Duy nhất toàn cục chứ không phải duy nhất trong từng tenant, vì màn hình đăng nhập
    /// chỉ hỏi email và mật khẩu. Cho phép trùng email giữa hai tenant thì hệ thống buộc
    /// phải hỏi thêm "bạn thuộc tổ chức nào" — một câu hỏi mà người dùng thường không trả
    /// lời đúng, và là cửa cho việc dò xem một email có tồn tại ở tenant nào.
    /// <para>
    /// Lưu chữ thường để so sánh không phụ thuộc hoa/thường. Không dùng
    /// <c>ILIKE</c> hay collation không phân biệt hoa thường: <c>InvariantGlobalization</c>
    /// đang bật (xem Directory.Build.props), nên so sánh chuỗi theo văn hoá là thứ không
    /// nên dựa vào ở đây.
    /// </para>
    /// </remarks>
    public required string Email { get; set; }

    /// <summary>
    /// Mật khẩu đã băm, ở dạng chuỗi tự mô tả.
    /// </summary>
    /// <remarks>
    /// Định dạng và lý do chọn nó: xem <see cref="Security.PasswordHasher"/>. Chuỗi mang
    /// theo cả thuật toán, số vòng lặp và muối, nên đổi tham số về sau không cần migration
    /// dữ liệu — bản ghi cũ vẫn kiểm tra được bằng tham số cũ của chính nó.
    /// </remarks>
    public required string PasswordHash { get; set; }

    /// <summary>Vai trò trong tenant: <c>admin</c> | <c>user</c> | <c>viewer</c>.</summary>
    /// <remarks>Đi vào claim <c>role</c>, khớp <see cref="Xnk.Shared.Tenancy.TenantClaims.Role"/>.</remarks>
    public required string Role { get; set; }

    /// <summary>
    /// Còn được phép đăng nhập hay không.
    /// </summary>
    /// <remarks>
    /// Vô hiệu hoá bằng cờ chứ không bằng xoá bản ghi: token đã phát vẫn còn hạn, và lịch
    /// sử hội thoại vẫn trỏ tới người dùng này.
    /// </remarks>
    public bool IsActive { get; set; } = true;

    /// <summary>Thời điểm tạo, theo UTC.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Tenant sở hữu.</summary>
    public Tenant? Tenant { get; set; }
}
