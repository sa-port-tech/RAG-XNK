using Microsoft.Extensions.Options;
using Xnk.WorkflowWorker.Handlers;

namespace Xnk.WorkflowWorker.Camunda;

/// <summary>
/// Vòng lặp lấy External Task từ Camunda và giao cho handler tương ứng.
/// </summary>
/// <remarks>
/// <para>
/// Đây là **External Task pattern**, không phải Java Delegate. Toàn bộ logic nằm ngoài
/// engine, nên service .NET và Python tham gia được, và ngày Camunda 7 kết thúc hỗ trợ thì
/// phần phải viết lại chỉ là lớp gọi REST này (docs/00 §1.2 điểm 4, docs/10 §0).
/// </para>
/// <para>
/// Lấy về theo lô rồi xử lý **tuần tự**. Với khối lượng của prototype, song song hoá chỉ
/// thêm một nguồn lỗi khó tái hiện mà không đổi được gì đo đếm được.
/// </para>
/// </remarks>
/// <param name="client">Lớp gọi REST API.</param>
/// <param name="handlers">Các handler đã đăng ký, mỗi cái một topic.</param>
/// <param name="options">Tham số kết nối.</param>
/// <param name="log">Nhật ký.</param>
public sealed class ExternalTaskWorker(
    CamundaClient client,
    IEnumerable<IExternalTaskHandler> handlers,
    IOptions<CamundaOptions> options,
    ILogger<ExternalTaskWorker> log) : BackgroundService
{
    /// <summary>
    /// Số lần thử lại khi handler ném ngoại lệ.
    /// </summary>
    /// <remarks>
    /// docs/10 §1.4 đặt retry theo từng topic (1–3 lần). Con số ở đây là mặc định cho lát
    /// cắt skeleton; khi bảng topic đầy đủ được cài, retry phải đọc từ chính bảng đó.
    /// </remarks>
    public const int SoLanThuLai = 2;

    /// <summary>Khoảng chờ giữa hai lần thử, tính bằng mili giây.</summary>
    public const int KhoangChoThuLaiMs = 30_000;

    private readonly Dictionary<string, IExternalTaskHandler> _theoTopic =
        handlers.ToDictionary(h => h.Topic, StringComparer.Ordinal);

    /// <summary>
    /// Định danh worker gửi kèm mỗi lời gọi.
    /// </summary>
    /// <remarks>
    /// Có tên máy để khi hai replica cùng chạy, log của engine nói được replica nào đang
    /// giữ khoá của task nào. Chuỗi ngẫu nhiên thuần thì không tra ngược ra được gì.
    /// </remarks>
    public string WorkerId { get; } = $"workflow-worker@{Environment.MachineName}";

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_theoTopic.Count == 0)
        {
            log.LogWarning("Không có handler nào được đăng ký — worker sẽ không lấy task nào.");
            return;
        }

        log.LogInformation(
            "Worker {WorkerId} bắt đầu, đăng ký {SoTopic} topic: {Topics}",
            WorkerId, _theoTopic.Count, string.Join(", ", _theoTopic.Keys));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MotLuotAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception loi)
            {
                // Engine chết thì worker KHÔNG được chết theo — nó phải chờ và thử lại.
                // Thoát vòng lặp ở đây nghĩa là container sống nhưng vĩnh viễn không làm gì,
                // và readiness vẫn xanh vì tiến trình vẫn chạy: loại hỏng hóc im lặng nhất.
                log.LogError(loi, "Lỗi khi lấy task, thử lại sau {Delay}s.", 5);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        log.LogInformation("Worker {WorkerId} dừng.", WorkerId);
    }

    private async Task MotLuotAsync(CancellationToken huy)
    {
        IReadOnlyList<ExternalTask> danhSach = await client.FetchAndLockAsync(
            WorkerId, _theoTopic.Keys, options.Value, huy);

        foreach (ExternalTask task in danhSach)
        {
            await XuLyMotTaskAsync(task, huy);
        }
    }

    private async Task XuLyMotTaskAsync(ExternalTask task, CancellationToken huy)
    {
        if (!_theoTopic.TryGetValue(task.TopicName, out IExternalTaskHandler? handler))
        {
            // Không thể xảy ra nếu fetchAndLock chỉ hỏi những topic đã đăng ký — nhưng nếu
            // xảy ra thì đừng giữ khoá task của người khác.
            log.LogWarning("Nhận task topic {Topic} không có handler — bỏ qua.", task.TopicName);
            return;
        }

        try
        {
            IReadOnlyDictionary<string, CamundaVariable>? bien = await handler.XuLyAsync(task, huy);
            await client.CompleteAsync(task.Id, WorkerId, bien, huy);

            log.LogInformation(
                "Hoàn thành task {TaskId} topic {Topic} của instance {Instance}.",
                task.Id, task.TopicName, task.ProcessInstanceId);
        }
        catch (Exception loi) when (loi is not OperationCanceledException)
        {
            // Retries hiện tại `null` nghĩa là task chưa từng lỗi → bắt đầu từ SoLanThuLai.
            // Hết lượt thì đặt 0, engine tạo incident và dừng instance ở trạng thái chờ can
            // thiệp — docs/10 §1.4: "Không tự động bỏ qua bước lỗi."
            int conLai = Math.Max((task.Retries ?? SoLanThuLai + 1) - 1, 0);

            log.LogError(
                loi,
                "Task {TaskId} topic {Topic} lỗi, còn {ConLai} lần thử.",
                task.Id, task.TopicName, conLai);

            await client.FailureAsync(
                task.Id, WorkerId, loi.Message, loi.ToString(), conLai, KhoangChoThuLaiMs, huy);
        }
    }
}
