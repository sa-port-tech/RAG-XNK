namespace Xnk.Corpus.Contracts;

/// <summary>Một trang kết quả.</summary>
/// <typeparam name="T">Kiểu phần tử.</typeparam>
/// <param name="Items">Các phần tử của trang hiện tại.</param>
/// <param name="Page">Số trang, bắt đầu từ 1.</param>
/// <param name="PageSize">Số phần tử mỗi trang.</param>
/// <param name="TotalCount">
/// Tổng số bản ghi <b>mà người gọi được phép thấy</b>.
/// </param>
/// <remarks>
/// Chữ "được phép thấy" ở trên không phải cách nói cho lịch sự: <c>TotalCount</c> đếm sau
/// khi bộ lọc tenant đã áp, nên hai tenant gọi cùng một URL sẽ nhận hai con số khác nhau.
/// Trả về tổng số thật của bảng sẽ rò rỉ quy mô dữ liệu của tenant khác — một kiểu rò rỉ
/// không lộ ra nội dung nào nhưng vẫn là rò rỉ.
/// </remarks>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount);

/// <summary>Văn bản, ở mức tóm tắt cho danh sách.</summary>
/// <param name="Id">Khoá chính.</param>
/// <param name="DocumentNumber">Số hiệu, ví dụ "39/2018/TT-BTC".</param>
/// <param name="Title">Trích yếu.</param>
/// <param name="EffectiveFrom">Ngày bắt đầu hiệu lực, <c>null</c> nếu chưa xác minh.</param>
/// <param name="EffectiveTo">Ngày hết hiệu lực, <c>null</c> nếu còn hiệu lực hoặc chưa xác minh.</param>
/// <param name="IsShared">
/// <c>true</c> nếu là văn bản quy phạm pháp luật dùng chung; <c>false</c> nếu là tài liệu
/// riêng của tenant.
/// </param>
/// <remarks>
/// DTO riêng chứ không trả thẳng thực thể EF. Không phải để cho đúng bài: thực thể có
/// trường <c>TenantId</c>, và trả nguyên nó ra ngoài nghĩa là mọi phản hồi đều mang theo
/// một định danh nội bộ mà client không cần — thứ về sau sẽ có người dùng để tự lọc ở phía
/// client, rồi lớp cách ly thật ở tầng SQL trở thành "chỉ là một trong hai chỗ lọc".
/// <para>
/// <see cref="IsShared"/> là thứ client thật sự cần biết (văn bản này của riêng tổ chức
/// mình hay của chung), và nó không tiết lộ tenant nào cả.
/// </para>
/// </remarks>
public sealed record DocumentSummary(
    Guid Id,
    string DocumentNumber,
    string Title,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsShared);
