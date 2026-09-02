using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Xnk.WorkflowWorker.Camunda;

/// <summary>Tham số kết nối Camunda, đọc từ section <c>Camunda</c>.</summary>
public sealed class CamundaOptions
{
    /// <summary>Tên section trong appsettings.</summary>
    public const string SectionName = "Camunda";

    /// <summary>Địa chỉ gốc REST API, ví dụ <c>http://camunda:8080/engine-rest</c>.</summary>
    [Required(AllowEmptyStrings = false)]
    [Url]
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Thời gian khoá một task sau khi lấy về, tính bằng mili giây.
    /// </summary>
    /// <remarks>
    /// Phải dài hơn thời gian xử lý dài nhất có thể. Ngắn quá thì engine coi là worker đã
    /// chết và giao task cho worker khác **trong khi worker này vẫn đang làm** — tức là
    /// cùng một bước chạy hai lần.
    /// </remarks>
    [Range(1_000, 600_000)]
    public int LockDurationMs { get; init; } = 60_000;

    /// <summary>
    /// Thời gian engine giữ kết nối chờ khi chưa có task nào, tính bằng mili giây.
    /// </summary>
    /// <remarks>
    /// Long polling: engine giữ request lại tới khi có task hoặc hết hạn, thay vì để worker
    /// hỏi liên tục. Đừng đặt quá dài — mỗi giây ở đây là một giây worker chậm phản ứng với
    /// tín hiệu dừng khi container bị thu hồi.
    /// </remarks>
    [Range(1_000, 60_000)]
    public int AsyncResponseTimeoutMs { get; init; } = 10_000;

    /// <summary>Số task lấy về mỗi lượt.</summary>
    [Range(1, 100)]
    public int MaxTasks { get; init; } = 5;
}

/// <summary>Một External Task lấy về từ engine.</summary>
/// <param name="Id">Định danh task.</param>
/// <param name="TopicName">Topic mà task thuộc về.</param>
/// <param name="ProcessInstanceId">Instance đang chạy.</param>
/// <param name="Retries">Số lần thử còn lại; <c>null</c> nghĩa là chưa từng lỗi.</param>
/// <param name="Variables">Biến process kèm theo.</param>
public sealed record ExternalTask(
    string Id,
    string TopicName,
    string ProcessInstanceId,
    int? Retries,
    IReadOnlyDictionary<string, CamundaVariable>? Variables)
{
    /// <summary>Đọc một biến dạng chuỗi, trả <c>null</c> nếu không có.</summary>
    public string? Chuoi(string ten) =>
        Variables is not null && Variables.TryGetValue(ten, out CamundaVariable? bien)
            ? bien.Value?.ToString()
            : null;
}

/// <summary>Giá trị một biến process.</summary>
/// <param name="Value">Giá trị thô.</param>
/// <param name="Type">Kiểu theo cách gọi của Camunda: String, Integer, Boolean, Json…</param>
public sealed record CamundaVariable(
    [property: JsonPropertyName("value")] object? Value,
    [property: JsonPropertyName("type")] string? Type);

/// <summary>
/// Gọi REST API của Camunda 7.
/// </summary>
/// <remarks>
/// Tự viết thay vì dùng thư viện client: bề mặt cần dùng chỉ có bốn lời gọi, và Camunda 7
/// đang trong lộ trình kết thúc hỗ trợ (docs/00 §1.2) — thêm một phụ thuộc vào hệ sinh thái
/// của nó là thêm một thứ phải gỡ khi chuyển đổi.
/// </remarks>
/// <param name="httpClient">Client đã cấu hình BaseAddress và thời gian chờ.</param>
public sealed class CamundaClient(HttpClient httpClient)
{
    /// <summary>Lấy và khoá các task thuộc những topic đã đăng ký.</summary>
    public async Task<IReadOnlyList<ExternalTask>> FetchAndLockAsync(
        string workerId,
        IReadOnlyCollection<string> topics,
        CamundaOptions options,
        CancellationToken huy)
    {
        ArgumentNullException.ThrowIfNull(options);

        var yeuCau = new
        {
            workerId,
            maxTasks = options.MaxTasks,
            usePriority = true,
            asyncResponseTimeout = options.AsyncResponseTimeoutMs,
            topics = topics.Select(t => new
            {
                topicName = t,
                lockDuration = options.LockDurationMs,
            }),
        };

        HttpResponseMessage phanHoi = await httpClient.PostAsJsonAsync(
            "external-task/fetchAndLock", yeuCau, huy);
        phanHoi.EnsureSuccessStatusCode();

        return await phanHoi.Content.ReadFromJsonAsync<List<ExternalTask>>(huy) ?? [];
    }

    /// <summary>Báo task đã xong, kèm biến ghi ngược vào process.</summary>
    public async Task CompleteAsync(
        string taskId,
        string workerId,
        IReadOnlyDictionary<string, CamundaVariable>? bien,
        CancellationToken huy)
    {
        HttpResponseMessage phanHoi = await httpClient.PostAsJsonAsync(
            $"external-task/{taskId}/complete",
            new { workerId, variables = bien },
            huy);

        phanHoi.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Báo task thất bại.
    /// </summary>
    /// <remarks>
    /// <paramref name="retries"/> bằng 0 thì engine tạo **incident** và dừng instance ở
    /// trạng thái chờ can thiệp — đúng yêu cầu của docs/10 §1.4: *"Không tự động bỏ qua
    /// bước lỗi."*
    /// </remarks>
    public async Task FailureAsync(
        string taskId,
        string workerId,
        string thongBao,
        string chiTiet,
        int retries,
        int retryTimeoutMs,
        CancellationToken huy)
    {
        HttpResponseMessage phanHoi = await httpClient.PostAsJsonAsync(
            $"external-task/{taskId}/failure",
            new
            {
                workerId,
                errorMessage = thongBao,
                errorDetails = chiTiet,
                retries,
                retryTimeout = retryTimeoutMs,
            },
            huy);

        phanHoi.EnsureSuccessStatusCode();
    }

    /// <summary>Hỏi phiên bản engine — phép kiểm nhẹ nhất chứng minh REST API sống.</summary>
    public async Task KiemTraKetNoiAsync(CancellationToken huy)
    {
        HttpResponseMessage phanHoi = await httpClient.GetAsync(new Uri("version", UriKind.Relative), huy);
        phanHoi.EnsureSuccessStatusCode();
    }
}
