using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xnk.WorkflowWorker.Camunda;
using Xnk.WorkflowWorker.Handlers;

namespace Xnk.WorkflowWorker.Tests;

/// <summary>Ghi lại request đi ra và trả phản hồi đã đặt trước.</summary>
public sealed class GhiLaiHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode Ma, string Json)> _kichBan = new();

    /// <summary>Các request đã đi qua, kèm thân đã đọc sẵn.</summary>
    public List<(string Url, string Than)> DaGoi { get; } = [];

    /// <summary>Đặt phản hồi cho lần gọi kế tiếp.</summary>
    public GhiLaiHandler TraVe(HttpStatusCode ma, string json = "{}")
    {
        _kichBan.Enqueue((ma, json));
        return this;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string than = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);

        DaGoi.Add((request.RequestUri!.ToString(), than));

        (HttpStatusCode ma, string json) = _kichBan.Count > 0
            ? _kichBan.Dequeue()
            : (HttpStatusCode.OK, "[]");

        return new HttpResponseMessage(ma)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }
}

/// <summary>Handler dùng cho test — thành công hoặc ném ngoại lệ theo yêu cầu.</summary>
internal sealed class HandlerThuNghiem(string topic, Exception? loi = null) : IExternalTaskHandler
{
    public string Topic { get; } = topic;

    public int SoLanGoi { get; private set; }

    public Task<IReadOnlyDictionary<string, CamundaVariable>?> XuLyAsync(
        ExternalTask task, CancellationToken huy)
    {
        SoLanGoi++;
        return loi is not null
            ? Task.FromException<IReadOnlyDictionary<string, CamundaVariable>?>(loi)
            : Task.FromResult<IReadOnlyDictionary<string, CamundaVariable>?>(null);
    }
}

/// <summary>Vòng lặp lấy và xử lý External Task.</summary>
public sealed class ExternalTaskWorkerTests : IDisposable
{
    private const string _topic = "corpus.notify-expert";

    /// <summary>
    /// Các <see cref="HttpClient"/> đã dựng trong test, để giải phóng khi xong.
    /// </summary>
    /// <remarks>
    /// Không dùng <c>using</c> tại chỗ được: client phải sống suốt vòng đời của
    /// <see cref="CamundaClient"/> mà test đang chạy. Gom lại rồi giải phóng ở cuối là cách
    /// giữ đúng vòng đời mà vẫn không rò socket qua từng test.
    /// </remarks>
    private readonly List<HttpClient> _clients = [];

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (HttpClient client in _clients)
        {
            client.Dispose();
        }
    }

    /// <summary>Một lô fetchAndLock chứa đúng một task.</summary>
    /// <remarks>
    /// Ghép chuỗi thay vì dùng raw string nội suy: JSON có sẵn nhiều dấu ngoặc nhọn liền
    /// nhau, và trộn với cú pháp <c>{{…}}</c> của C# cho ra một biểu thức mà trình biên
    /// dịch từ chối với thông báo khó đọc hơn hẳn cái nó thay thế.
    /// </remarks>
    private static string MotTask(int? retries = null)
    {
        string soLan = retries?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null";

        return "[{\"id\":\"task-1\",\"topicName\":\"" + _topic + "\","
            + "\"processInstanceId\":\"inst-1\",\"retries\":" + soLan + ","
            + "\"variables\":{"
            + "\"van_ban_id\":{\"value\":\"39/2018/TT-BTC\",\"type\":\"String\"},"
            + "\"nguoi_duyet\":{\"value\":\"chi.le@cangxanh.vn\",\"type\":\"String\"}}}]";
    }

    private (ExternalTaskWorker Worker, GhiLaiHandler Http, HandlerThuNghiem Handler) Dung(
        GhiLaiHandler http, Exception? loiHandler = null)
    {
        var httpClient = new HttpClient(http)
        {
            BaseAddress = new Uri("http://camunda.test/engine-rest/"),
        };
        _clients.Add(httpClient);

        var client = new CamundaClient(httpClient);

        var handler = new HandlerThuNghiem(_topic, loiHandler);
        var options = Options.Create(new CamundaOptions
        {
            BaseUrl = "http://camunda.test/engine-rest",
            AsyncResponseTimeoutMs = 1_000,
        });

        return (
            new ExternalTaskWorker(
                client,
                [handler],
                options,
                new NhipTimWorker(TimeProvider.System),
                NullLogger<ExternalTaskWorker>.Instance),
            http,
            handler);
    }

    private static async Task ChayMotLuot(ExternalTaskWorker worker)
    {
        // Cho vòng lặp chạy rồi dừng: đủ để nó lấy một lô và xử lý xong.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await worker.StartAsync(cts.Token);
        await Task.Delay(300, CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Lay_task_thanh_cong_thi_goi_complete()
    {
        var http = new GhiLaiHandler().TraVe(HttpStatusCode.OK, MotTask()).TraVe(HttpStatusCode.NoContent);
        (ExternalTaskWorker worker, _, HandlerThuNghiem handler) = Dung(http);

        await ChayMotLuot(worker);

        Assert.Equal(1, handler.SoLanGoi);
        Assert.Contains(http.DaGoi, g => g.Url.EndsWith("external-task/task-1/complete", StringComparison.Ordinal));
        Assert.DoesNotContain(http.DaGoi, g => g.Url.Contains("/failure", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FetchAndLock_gui_dung_topic_va_thoi_gian_khoa()
    {
        var http = new GhiLaiHandler().TraVe(HttpStatusCode.OK, "[]");
        (ExternalTaskWorker worker, _, _) = Dung(http);

        await ChayMotLuot(worker);

        (string Url, string Than) goi = http.DaGoi[0];
        Assert.EndsWith("external-task/fetchAndLock", goi.Url, StringComparison.Ordinal);

        using JsonDocument than = JsonDocument.Parse(goi.Than);
        Assert.Equal(_topic, than.RootElement.GetProperty("topics")[0].GetProperty("topicName").GetString());
        Assert.Equal(60_000, than.RootElement.GetProperty("topics")[0].GetProperty("lockDuration").GetInt32());
        Assert.Equal(worker.WorkerId, than.RootElement.GetProperty("workerId").GetString());
    }

    [Fact]
    public async Task Handler_nem_ngoai_le_thi_bao_LOI_chu_khong_hoan_thanh()
    {
        // Nuốt ngoại lệ rồi complete là cách để process chạy tiếp như thể bước đó đã xong.
        var http = new GhiLaiHandler().TraVe(HttpStatusCode.OK, MotTask()).TraVe(HttpStatusCode.NoContent);
        (ExternalTaskWorker worker, _, _) = Dung(http, new InvalidOperationException("hỏng"));

        await ChayMotLuot(worker);

        Assert.Contains(http.DaGoi, g => g.Url.EndsWith("external-task/task-1/failure", StringComparison.Ordinal));
        Assert.DoesNotContain(http.DaGoi, g => g.Url.Contains("/complete", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Lan_loi_dau_tien_dat_so_lan_thu_con_lai_dung_bang_SoLanThuLai()
    {
        // retries = null nghĩa là task chưa từng lỗi.
        var http = new GhiLaiHandler().TraVe(HttpStatusCode.OK, MotTask()).TraVe(HttpStatusCode.NoContent);
        (ExternalTaskWorker worker, _, _) = Dung(http, new InvalidOperationException("hỏng"));

        await ChayMotLuot(worker);

        string than = http.DaGoi.First(g => g.Url.Contains("/failure", StringComparison.Ordinal)).Than;
        using JsonDocument doc = JsonDocument.Parse(than);
        Assert.Equal(ExternalTaskWorker.SoLanThuLai, doc.RootElement.GetProperty("retries").GetInt32());
    }

    [Fact]
    public async Task Het_luot_thu_thi_dat_retries_ve_0_de_engine_tao_incident()
    {
        // docs/10 §1.4: "Không tự động bỏ qua bước lỗi." retries = 0 làm engine dừng
        // instance ở trạng thái chờ can thiệp thay vì để nó trôi tiếp.
        var http = new GhiLaiHandler().TraVe(HttpStatusCode.OK, MotTask(retries: 1)).TraVe(HttpStatusCode.NoContent);
        (ExternalTaskWorker worker, _, _) = Dung(http, new InvalidOperationException("hỏng"));

        await ChayMotLuot(worker);

        string than = http.DaGoi.First(g => g.Url.Contains("/failure", StringComparison.Ordinal)).Than;
        using JsonDocument doc = JsonDocument.Parse(than);
        Assert.Equal(0, doc.RootElement.GetProperty("retries").GetInt32());
    }

    [Fact]
    public async Task Engine_khong_goi_duoc_thi_worker_KHONG_chet()
    {
        // Thoát vòng lặp khi engine hỏng nghĩa là container sống nhưng vĩnh viễn không làm
        // gì — và readiness vẫn xanh vì tiến trình vẫn chạy. Loại hỏng hóc im lặng nhất.
        var http = new GhiLaiHandler()
            .TraVe(HttpStatusCode.InternalServerError)
            .TraVe(HttpStatusCode.InternalServerError);
        (ExternalTaskWorker worker, _, _) = Dung(http);

        await ChayMotLuot(worker);

        // Không ném ra ngoài, và đã thử gọi ít nhất một lần.
        Assert.NotEmpty(http.DaGoi);
    }
}

/// <summary>Handler thông báo chuyên gia.</summary>
public sealed class NotifyExpertHandlerTests
{
    [Fact]
    public async Task Khong_ghi_bien_nao_nguoc_vao_process()
    {
        // docs/10 §1.4 khai topic corpus.notify-expert có Output là "—". Trả về một biến
        // nào đó ở đây là bịa ra dữ liệu nghiệp vụ mà đặc tả không có.
        var handler = new NotifyExpertHandler(NullLogger<NotifyExpertHandler>.Instance);
        var task = new ExternalTask("t", "corpus.notify-expert", "inst", null, null);

        IReadOnlyDictionary<string, CamundaVariable>? bien =
            await handler.XuLyAsync(task, CancellationToken.None);

        Assert.Null(bien);
    }

    [Fact]
    public void Doc_duoc_bien_process_theo_dung_ten_trong_docs_10()
    {
        var task = new ExternalTask("t", "corpus.notify-expert", "inst", null,
            new Dictionary<string, CamundaVariable>
            {
                ["van_ban_id"] = new("39/2018/TT-BTC", "String"),
                ["nguoi_duyet"] = new("chi.le@cangxanh.vn", "String"),
            });

        Assert.Equal("39/2018/TT-BTC", task.Chuoi("van_ban_id"));
        Assert.Equal("chi.le@cangxanh.vn", task.Chuoi("nguoi_duyet"));
        Assert.Null(task.Chuoi("khong_ton_tai"));
    }
}
