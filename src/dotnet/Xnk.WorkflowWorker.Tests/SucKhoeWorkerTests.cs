using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xnk.WorkflowWorker.Camunda;
using Xnk.WorkflowWorker.Handlers;

namespace Xnk.WorkflowWorker.Tests;

/// <summary>Đồng hồ tự tay đẩy, để test không phải chờ thật.</summary>
/// <remarks>
/// Mười dòng thay cho một phụ thuộc mới (<c>Microsoft.Extensions.TimeProvider.Testing</c>):
/// thứ duy nhất cần ở đây là <c>GetUtcNow</c>, và PR này vốn đã có phần nợ về tính tái lập
/// của gói NuGet — thêm một gói nữa cho mười dòng là đi sai hướng.
/// </remarks>
internal sealed class DongHoGia : TimeProvider
{
    private DateTimeOffset _bayGio = new(2026, 9, 10, 0, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _bayGio;

    public void TienThem(TimeSpan khoang) => _bayGio = _bayGio.Add(khoang);
}

/// <summary>
/// Worker phải CHẾT khi nó không thể làm việc, và health check phải nói được sự thật đó.
/// </summary>
/// <remarks>
/// Cả hai test ở đây nhắm vào cùng một loại hỏng hóc: <b>container sống, bảng điều khiển
/// xanh, và không một task nào được xử lý</b>. Chính khối catch trong
/// <c>ExternalTaskWorker</c> gọi nó là "loại hỏng hóc im lặng nhất" — nhưng trước đây mã
/// lại có hai đường dẫn thẳng tới đúng trạng thái ấy.
/// </remarks>
public sealed class SucKhoeWorkerTests
{
    private const string _topic = "corpus.notify-expert";

    /// <summary>Handler cố tình chạy lâu hơn cửa sổ khoá.</summary>
    private sealed class HandlerCham(string topic) : IExternalTaskHandler
    {
        public string Topic { get; } = topic;

        public async Task<IReadOnlyDictionary<string, CamundaVariable>?> XuLyAsync(
            ExternalTask task, CancellationToken huy)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), huy);
            return null;
        }
    }

    private static string MotTaskJson() =>
        "[{\"id\":\"task-1\",\"topicName\":\"" + _topic + "\","
        + "\"processInstanceId\":\"inst-1\",\"retries\":null,\"variables\":{}}]";

    private static CamundaOptions ThamSo() => new()
    {
        BaseUrl = "http://camunda.test/engine-rest",
        AsyncResponseTimeoutMs = 1_000,
    };

    [Fact]
    public async Task Handler_chay_qua_cua_so_khoa_thi_bao_HONG_chu_khong_chay_trung()
    {
        // ⚠️ `fetchAndLock` khoá tối đa MaxTasks task trong LockDurationMs. Ba tham số —
        // số task mỗi lô, thời hạn khoá, thời gian một handler chạy — trước đây không có
        // gì ràng buộc với nhau: năm task tuần tự, mỗi task mười lăm giây, là quá cửa sổ
        // sáu mươi giây ở task thứ tư. Engine coi worker đã chết và giao lại cho worker
        // khác TRONG KHI worker này vẫn đang làm. Cùng một bước chạy hai lần, không lỗi
        // nào được ghi ở đâu cả.
        //
        // Nay handler chạy dưới ngân sách bằng phần khoá còn lại; quá thì task được đánh
        // hỏng tường minh (engine ghi lại, giảm retries) thay vì rơi vào chạy trùng im
        // lặng. Ở đây LockDurationMs = 1s nên ngân sách là 800ms, còn handler chờ 5s.
        var thamSo = new CamundaOptions
        {
            BaseUrl = "http://camunda.test/engine-rest",
            AsyncResponseTimeoutMs = 1_000,
            LockDurationMs = 1_000,
        };

        using var http = new GhiLaiHandler()
            .TraVe(HttpStatusCode.OK, MotTaskJson())
            .TraVe(HttpStatusCode.NoContent);
        using var httpClient = new HttpClient(http)
        {
            BaseAddress = new Uri("http://camunda.test/engine-rest/"),
        };

        var worker = new ExternalTaskWorker(
            new CamundaClient(httpClient),
            [new HandlerCham(_topic)],
            Options.Create(thamSo),
            new NhipTimWorker(TimeProvider.System),
            NullLogger<ExternalTaskWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StartAsync(cts.Token);
        await Task.Delay(2_000, CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        // Lời gọi thứ hai phải là `failure`, KHÔNG phải `complete`: task quá hạn khoá là
        // một sự cố cần engine biết, không phải một task hoàn thành muộn.
        Assert.Contains(http.DaGoi, g => g.Url.EndsWith("/failure", StringComparison.Ordinal));
        Assert.DoesNotContain(http.DaGoi, g => g.Url.EndsWith("/complete", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Khong_co_handler_nao_thi_worker_NEM_chu_khong_chay_khong()
    {
        // ⚠️ HỒI QUY. Bản trước ghi log cảnh báo rồi `return`, tức là ExecuteAsync kết thúc
        // trong khi host vẫn chạy: container sống, readiness xanh, không lấy task nào —
        // mãi mãi. Một worker không có handler không phải worker rảnh; nó là một cấu hình
        // sai đã lọt qua khâu deploy, và nó phải làm deploy đỏ.
        using var http = new GhiLaiHandler();
        using var httpClient = new HttpClient(http)
        {
            BaseAddress = new Uri("http://camunda.test/engine-rest/"),
        };

        var worker = new ExternalTaskWorker(
            new CamundaClient(httpClient),
            [],
            Options.Create(ThamSo()),
            new NhipTimWorker(TimeProvider.System),
            NullLogger<ExternalTaskWorker>.Instance);

        InvalidOperationException loi = await Assert.ThrowsAsync<InvalidOperationException>(
            () => worker.StartAsync(CancellationToken.None));

        Assert.Contains("IExternalTaskHandler", loi.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Engine_goi_duoc_nhung_vong_lap_dung_thi_readiness_DO()
    {
        // Vế mà bản trước của CamundaHealthCheck thiếu hẳn. Nó chỉ hỏi "gọi được Camunda
        // không" — một câu hỏi về ENGINE, trong khi readiness của worker phải trả lời một
        // câu hỏi về CHÍNH NÓ. Hai điều kiện độc lập, và ở đây engine hoàn toàn khoẻ.
        var dongHo = new DongHoGia();
        using var http = new GhiLaiHandler().TraVe(HttpStatusCode.OK, "{}");
        using var httpClient = new HttpClient(http)
        {
            BaseAddress = new Uri("http://camunda.test/engine-rest/"),
        };

        var nhipTim = new NhipTimWorker(dongHo);
        nhipTim.BatDau();

        var kiem = new CamundaHealthCheck(
            new CamundaClient(httpClient), nhipTim, Options.Create(ThamSo()), dongHo);

        // Im lặng quá ngưỡng = AsyncResponseTimeoutMs × HeSoNguongImLang.
        dongHo.TienThem(TimeSpan.FromMilliseconds(
            ThamSo().AsyncResponseTimeoutMs * CamundaHealthCheck.HeSoNguongImLang + 1_000));

        HealthCheckResult ketQua = await kiem.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, ketQua.Status);
        Assert.Contains("im lặng", ketQua.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Vong_lap_dang_quay_thi_readiness_XANH()
    {
        var dongHo = new DongHoGia();
        using var http = new GhiLaiHandler().TraVe(HttpStatusCode.OK, "{}");
        using var httpClient = new HttpClient(http)
        {
            BaseAddress = new Uri("http://camunda.test/engine-rest/"),
        };

        var nhipTim = new NhipTimWorker(dongHo);
        nhipTim.BatDau();

        var kiem = new CamundaHealthCheck(
            new CamundaClient(httpClient), nhipTim, Options.Create(ThamSo()), dongHo);

        // Một lượt long polling bình thường vẫn nằm trong ngưỡng.
        dongHo.TienThem(TimeSpan.FromMilliseconds(ThamSo().AsyncResponseTimeoutMs));
        nhipTim.Dap();

        HealthCheckResult ketQua = await kiem.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, ketQua.Status);
    }

    [Fact]
    public async Task Chua_khoi_dong_vong_lap_thi_readiness_DO()
    {
        var dongHo = new DongHoGia();
        using var http = new GhiLaiHandler().TraVe(HttpStatusCode.OK, "{}");
        using var httpClient = new HttpClient(http)
        {
            BaseAddress = new Uri("http://camunda.test/engine-rest/"),
        };

        var kiem = new CamundaHealthCheck(
            new CamundaClient(httpClient),
            new NhipTimWorker(dongHo),
            Options.Create(ThamSo()),
            dongHo);

        HealthCheckResult ketQua = await kiem.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, ketQua.Status);
    }
}
