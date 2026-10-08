using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Xnk.Chat.Contracts;

/// <summary>Câu hỏi của người dùng.</summary>
/// <param name="Question">Nội dung câu hỏi.</param>
public sealed record ChatRequest(
    [property: Required(AllowEmptyStrings = false)]
    [property: MaxLength(2000)]
    string Question);

/// <summary>Một văn bản được dùng làm căn cứ cho câu trả lời.</summary>
/// <param name="DocumentNumber">Số hiệu, ví dụ "39/2018/TT-BTC".</param>
/// <param name="Title">Trích yếu.</param>
/// <param name="IsShared">Văn bản quy phạm dùng chung, hay tài liệu riêng của tenant.</param>
/// <remarks>
/// <para>
/// ⚠️ Kiểu này KHÔNG phải trích dẫn, và tên nó nói đúng điều đó.
/// </para>
/// <para>
/// Nó từng tên là <c>TaiLieuThamKhao</c>. Nhưng <c>RetrievalClient.LayTaiLieuThamKhaoAsync</c>
/// lấy <b>N văn bản đầu theo số hiệu</b> — câu hỏi thậm chí không phải tham số của hàm đó
/// (xem <c>docs/20</c> §3: không embedding, không tìm kiếm ngữ nghĩa, không BM25, không
/// rerank, không lọc hiệu lực). Gọi kết quả ấy là "trích dẫn" là đặt cho một danh sách
/// tuỳ tiện cái tên của thứ mà cả sản phẩm này tồn tại để làm cho đúng.
/// </para>
/// <para>
/// Cái tên là chỗ rẻ nhất để nói thật. Một client đọc trường <c>citations</c> sẽ hiển thị
/// nó như căn cứ pháp lý cho người dùng; đọc <c>referencedDocuments</c> thì không. Đổi lại
/// tên khi có retrieval thật (epic E2/E7) là một PR; đổi lại niềm tin của người dùng đã
/// thấy một trích dẫn sai thì không.
/// </para>
/// <para>
/// Trả kèm câu trả lời **luôn**, kể cả khi danh sách rỗng. Một câu trả lời không có căn cứ
/// nào mà trông giống hệt câu trả lời có căn cứ là đúng thứ hệ thống này tồn tại để tránh
/// (docs/00 §10.3).
/// </para>
/// </remarks>
public sealed record TaiLieuThamKhao(string DocumentNumber, string Title, bool IsShared);

/// <summary>Câu trả lời kèm căn cứ.</summary>
/// <param name="Answer">Nội dung câu trả lời do mô hình sinh.</param>
/// <param name="Model">Tên mô hình đã sinh ra nó.</param>
/// <param name="ReferencedDocuments">
/// Các văn bản đã được đưa cho mô hình làm ngữ cảnh. <b>Không phải</b> trích dẫn —
/// xem <see cref="TaiLieuThamKhao"/>.
/// </param>
/// <remarks>
/// <see cref="Model"/> đi ra ngoài có chủ đích: câu trả lời của một mô hình 3B chạy trên máy
/// dev và của Claude trên Bedrock khác nhau rất xa, và người đọc cần biết mình đang xem cái
/// nào trước khi kết luận chất lượng hệ thống.
/// </remarks>
public sealed record ChatResponse(
    string Answer,
    string Model,
    IReadOnlyList<TaiLieuThamKhao> ReferencedDocuments);

/// <summary>Một văn bản trong phản hồi của <c>retrieval</c>.</summary>
/// <remarks>
/// Tên trường snake_case vì <c>retrieval</c> là service Python và FastAPI tuần tự hoá theo
/// đúng tên thuộc tính của nó. Khai <see cref="JsonPropertyNameAttribute"/> ở đây thay vì
/// đổi quy ước JSON toàn cục của service này: hợp đồng thuộc về **phía kia**, và ép nó
/// theo quy ước của .NET là bắt một service phải biết về sở thích của service gọi nó.
/// </remarks>
internal sealed record RetrievalDocument(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("document_number")] string DocumentNumber,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("is_shared")] bool IsShared);

/// <summary>Trang kết quả của <c>retrieval</c>.</summary>
internal sealed record RetrievalPage(
    [property: JsonPropertyName("items")] IReadOnlyList<RetrievalDocument> Items,
    [property: JsonPropertyName("total_count")] int TotalCount);

/// <summary>Yêu cầu gửi sang <c>generation</c>.</summary>
internal sealed record GenerationRequest(
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("context")] IReadOnlyList<string> Context);

/// <summary>Phản hồi của <c>generation</c>.</summary>
internal sealed record GenerationResponse(
    [property: JsonPropertyName("answer")] string Answer,
    [property: JsonPropertyName("model")] string Model);
