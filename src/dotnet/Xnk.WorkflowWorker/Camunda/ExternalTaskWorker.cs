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
    NhipTimWorker nhipTim,
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
            // ⚠️ NÉM, không `return`.
            //
            // `return` ở đây kết thúc ExecuteAsync trong khi host vẫn chạy: container sống,
            // health vẫn xanh, và không một task nào được lấy — mãi mãi. Đó đúng là thứ mà
            // khối catch cách đây hai mươi dòng gọi tên là "loại hỏng hóc im lặng nhất",
            // chỉ khác là nó xảy ra ở ngay cửa vào thay vì ở giữa vòng lặp.
            //
            // Ném làm host dừng (BackgroundServiceExceptionBehavior.StopHost là mặc định từ
            // .NET 6), nên ECS thấy task chết và báo deploy hỏng — thông tin đúng, sớm.
            // Một worker không có handler nào không phải một worker rảnh rỗi; nó là một
            // cấu hình sai đã lọt qua khâu deploy.
            throw new InvalidOperationException(
                "Không có IExternalTaskHandler nào được đăng ký. Worker sẽ không bao giờ "
                + "lấy được task, nên nó dừng thay vì chạy không.");
        }

        nhipTim.BatDau();

        log.LogInformation(
            "Worker {WorkerId} bắt đầu, đăng ký {SoTopic} topic: {Topics}",
            WorkerId, _theoTopic.Count, string.Join(", ", _theoTopic.Keys));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MotLuotAsync(stoppingToken);

                // Đập cả khi lượt vừa rồi không có task nào: thứ đang đo là vòng lặp còn
                // quay, không phải có việc để làm.
                nhipTim.Dap();
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
        DateTimeOffset lucKhoa = nhipTim.BayGio;

        IReadOnlyList<ExternalTask> danhSach = await client.FetchAndLockAsync(
            WorkerId, _theoTopic.Keys, options.Value, huy);

        foreach (ExternalTask task in danhSach)
        {
            // ⚠️ Cửa sổ khoá là của CẢ LÔ, không phải của từng task.
            //
            // `fetchAndLock` khoá tối đa `MaxTasks` task trong `LockDurationMs`, và ba
            // tham số — số task mỗi lô, thời hạn khoá, thời gian một handler chạy — trước
            // đây không có gì ràng buộc với nhau. Năm task tuần tự, mỗi task mười lăm giây,
            // là quá cửa sổ sáu mươi giây ở task thứ tư: engine coi worker đã chết và giao
            // lại cho worker khác **trong khi worker này vẫn đang làm**. Cùng một bước chạy
            // hai lần, không lỗi nào được ghi ở đâu cả.
            //
            // Nên mỗi handler chạy dưới một ngân sách bằng phần khoá CÒN LẠI. Quá ngân
            // sách thì task được đánh hỏng một cách tường minh (engine ghi lỗi, giảm
            // retries) thay vì rơi vào chạy trùng im lặng.
            TimeSpan conLai = NganSachConLai(lucKhoa);
            if (conLai <= TimeSpan.Zero)
            {
                log.LogWarning(
                    "Hết cửa sổ khoá trước khi xử lý task {TaskId} — bỏ phần còn lại của lô "
                    + "để engine giao lại. Cân nhắc giảm MaxTasks hoặc tăng LockDurationMs.",
                    task.Id);
                return;
            }

            await XuLyMotTaskAsync(task, conLai, huy);
        }
    }

    /// <summary>Phần thời hạn khoá còn lại, trừ đi một biên an toàn.</summary>
    /// <remarks>
    /// Biên 20%: gọi <c>complete</c> cũng tốn thời gian, và hoàn thành handler đúng vào
    /// mili giây cuối của cửa sổ vẫn có thể thua engine ở bước báo kết quả.
    /// </remarks>
    private TimeSpan NganSachConLai(DateTimeOffset lucKhoa)
    {
        TimeSpan tron = TimeSpan.FromMilliseconds(options.Value.LockDurationMs * 0.8);
        return lucKhoa + tron - nhipTim.BayGio;
    }

    private async Task XuLyMotTaskAsync(ExternalTask task, TimeSpan nganSach, CancellationToken huy)
    {
        if (!_theoTopic.TryGetValue(task.TopicName, out IExternalTaskHandler? handler))
        {
            // Không thể xảy ra nếu fetchAndLock chỉ hỏi những topic đã đăng ký — nhưng nếu
            // xảy ra thì đừng giữ khoá task của người khác.
            log.LogWarning("Nhận task topic {Topic} không có handler — bỏ qua.", task.TopicName);
            return;
        }

        // Ngân sách của handler là phần khoá còn lại; quá thì huỷ và để khối catch bên
        // dưới báo hỏng cho engine. Truyền `huy` trần vào handler nghĩa là không có gì
        // ngăn nó chạy quá cửa sổ khoá.
        using var theoNganSach = CancellationTokenSource.CreateLinkedTokenSource(huy);
        theoNganSach.CancelAfter(nganSach);

        try
        {
            IReadOnlyDictionary<string, CamundaVariable>? bien =
                await handler.XuLyAsync(task, theoNganSach.Token);

            // `complete` dùng `huy`, không dùng token ngân sách: handler đã xong việc rồi,
            // huỷ ở bước báo kết quả chỉ tạo ra một task chạy xong mà engine không biết.
            await client.CompleteAsync(task.Id, WorkerId, bien, huy);

            log.LogInformation(
                "Hoàn thành task {TaskId} topic {Topic} của instance {Instance}.",
                task.Id, task.TopicName, task.ProcessInstanceId);
        }
        catch (OperationCanceledException) when (!huy.IsCancellationRequested)
        {
            // Quá ngân sách khoá — KHÔNG phải worker đang dừng. Báo hỏng tường minh để
            // engine giảm retries và ghi lại, thay vì im lặng thả task cho worker khác.
            log.LogError(
                "Task {TaskId} topic {Topic} vượt ngân sách {NganSach}s của cửa sổ khoá.",
                task.Id, task.TopicName, nganSach.TotalSeconds);

            int conLaiSauLoi = Math.Max((task.Retries ?? SoLanThuLai + 1) - 1, 0);
            string thongBao = $"Vượt ngân sách {nganSach.TotalSeconds:F0}s của cửa sổ khoá.";
            await client.FailureAsync(
                task.Id,
                WorkerId,
                thongBao,
                $"{thongBao} Cân nhắc giảm MaxTasks ({options.Value.MaxTasks}) hoặc tăng "
                    + $"LockDurationMs ({options.Value.LockDurationMs}).",
                conLaiSauLoi,
                KhoangChoThuLaiMs,
                huy);
            return;
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
