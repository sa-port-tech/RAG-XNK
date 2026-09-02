using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xnk.Corpus.Contracts;
using Xnk.Corpus.Data;

namespace Xnk.Corpus.Endpoints;

/// <summary>
/// API tra cứu văn bản.
/// </summary>
/// <remarks>
/// <b>Đây là lát cắt mẫu cho mọi story sau.</b> Vòng đời đầy đủ của một endpoint trong dự
/// án này: route → xác thực → ngữ cảnh tenant → EF → DTO → test. Chép nó khi thêm endpoint
/// mới, và chú ý điều nó <i>không</i> làm ở phần dưới.
/// </remarks>
public static class DocumentsEndpoints
{
    private const int KichThuocTrangMacDinh = 20;
    private const int KichThuocTrangToiDa = 100;

    /// <summary>Gắn nhóm endpoint văn bản vào ứng dụng.</summary>
    public static IEndpointRouteBuilder MapDocumentsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder nhom = endpoints.MapGroup("/documents")
            // Đặt ở cấp NHÓM, không phải từng endpoint: thêm một route mới vào nhóm này
            // sau đó mà quên gắn [Authorize] là chuyện sẽ xảy ra, và endpoint hở đó không
            // hiện ra trong diff dưới dạng gì cả — nó chỉ là một dòng MapGet trông bình
            // thường.
            .RequireAuthorization()
            .WithTags("documents");

        nhom.MapGet("/", LietKe)
            .WithName("LietKeVanBan")
            .WithSummary("Liệt kê văn bản mà tenant hiện tại được xem")
            .Produces<PagedResult<DocumentSummary>>(StatusCodes.Status200OK);

        nhom.MapGet("/{id:guid}", LayTheoId)
            .WithName("LayVanBanTheoId")
            .WithSummary("Lấy một văn bản theo khoá chính")
            .Produces<DocumentSummary>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return nhom;
    }

    /// <remarks>
    /// ⚠️ Chú ý thứ KHÔNG có trong hàm này: <b>không một dòng nào lọc theo tenant.</b>
    /// <para>
    /// Bộ lọc nằm trong global query filter của <see cref="CorpusDbContext"/> và được EF
    /// chèn vào mệnh đề WHERE của mọi truy vấn trên <c>Documents</c> — kể cả truy vấn mà
    /// người viết quên nghĩ tới nó (ADR-012). Thêm một điều kiện tenant ở đây là **có
    /// hại**, không phải thừa: nó tạo ấn tượng rằng lọc là việc của tầng ứng dụng, và
    /// endpoint tiếp theo sẽ được viết với niềm tin đó rồi quên mất một chỗ.
    /// </para>
    /// <para>
    /// Muốn thấy nó chạy thật: <c>DocumentsApiTests</c> gọi đúng URL này bằng token của
    /// hai tenant khác nhau và nhận hai kết quả khác nhau.
    /// </para>
    /// </remarks>
    private static async Task<IResult> LietKe(
        CorpusDbContext db,
        CancellationToken huy,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = KichThuocTrangMacDinh)
    {
        if (page < 1)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["page"] = ["Số trang phải từ 1 trở lên."],
            });
        }

        if (pageSize is < 1 or > KichThuocTrangToiDa)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["pageSize"] = [$"Kích thước trang phải từ 1 tới {KichThuocTrangToiDa}."],
            });
        }

        IQueryable<Document> truyVan = db.Documents.AsNoTracking();

        int tong = await truyVan.CountAsync(huy);

        List<DocumentSummary> muc = await truyVan
            // Thứ tự phải TẤT ĐỊNH, nếu không thì phân trang trùng lặp và bỏ sót bản ghi
            // một cách ngẫu nhiên: PostgreSQL không hứa hẹn thứ tự nào khi không có ORDER BY.
            // Thêm Id làm khoá phụ vì số hiệu không duy nhất (một văn bản có thể có bản
            // hợp nhất mang số hiệu khác nhưng cùng nội dung).
            .OrderBy(d => d.DocumentNumber)
            .ThenBy(d => d.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DocumentSummary(
                d.Id,
                d.DocumentNumber,
                d.Title,
                d.EffectiveFrom,
                d.EffectiveTo,
                d.TenantId == null))
            .ToListAsync(huy);

        return TypedResults.Ok(new PagedResult<DocumentSummary>(muc, page, pageSize, tong));
    }

    /// <remarks>
    /// Văn bản của tenant khác cho ra <b>404, không phải 403</b>. Đó là lựa chọn có chủ
    /// đích: 403 xác nhận rằng bản ghi đó tồn tại, và một người kiên nhẫn có thể dò ra
    /// danh sách khoá chính hợp lệ của tenant khác chỉ bằng cách đọc mã trạng thái.
    /// <para>
    /// Ở đây không cần viết dòng nào để đạt điều đó: global query filter làm bản ghi biến
    /// mất khỏi truy vấn, nên nhánh "không tìm thấy" chạy một cách tự nhiên.
    /// </para>
    /// </remarks>
    private static async Task<IResult> LayTheoId(
        Guid id,
        CorpusDbContext db,
        CancellationToken huy)
    {
        DocumentSummary? vanBan = await db.Documents
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DocumentSummary(
                d.Id,
                d.DocumentNumber,
                d.Title,
                d.EffectiveFrom,
                d.EffectiveTo,
                d.TenantId == null))
            .SingleOrDefaultAsync(huy);

        return vanBan is null
            ? TypedResults.Problem(
                title: "Không tìm thấy văn bản",
                detail: $"Không có văn bản với mã {id}.",
                statusCode: StatusCodes.Status404NotFound)
            : TypedResults.Ok(vanBan);
    }
}
