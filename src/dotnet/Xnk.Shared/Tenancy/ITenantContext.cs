namespace Xnk.Shared.Tenancy;

/// <summary>
/// Tenant của request đang được xử lý.
/// </summary>
/// <remarks>
/// <para>
/// Tồn tại như một trừu tượng thay vì đọc thẳng <c>HttpContext.User</c> ở nơi cần, vì
/// bộ lọc cách ly tenant nằm trong <c>DbContext</c> — và <c>DbContext</c> không nên biết
/// gì về HTTP. Nhờ vậy test cách ly tenant dựng được ngữ cảnh giả mà không cần một máy
/// chủ web, và cùng một bộ lọc chạy được cả trong worker nền lẫn trong API.
/// </para>
/// <para>
/// ⚠️ Đây là một phần của ranh giới bảo mật (docs/00 §12). Đừng thêm setter công khai:
/// tenant phải đến từ token đã xác thực, không phải từ tham số mà người gọi tự đặt.
/// </para>
/// </remarks>
public interface ITenantContext
{
    /// <summary>
    /// Tenant hiện tại, hoặc <c>null</c> khi chạy ngoài ngữ cảnh một request đã xác thực
    /// (job nền, migration, công cụ dòng lệnh).
    /// </summary>
    /// <remarks>
    /// <c>null</c> KHÔNG có nghĩa là "xem được mọi tenant". Bộ lọc trong
    /// <c>CorpusDbContext</c> hiểu <c>null</c> là "chỉ thấy phần dùng chung" — chọn như
    /// vậy để một lỗi thiếu ngữ cảnh biến thành mất dữ liệu nhìn thấy được, chứ không
    /// thành rò rỉ dữ liệu im lặng.
    /// </remarks>
    Guid? TenantId { get; }
}
