using Xnk.WorkflowWorker.Camunda;

namespace Xnk.WorkflowWorker.Handlers;

/// <summary>Xử lý một loại External Task.</summary>
public interface IExternalTaskHandler
{
    /// <summary>Topic mà handler này đăng ký, theo quy ước <c>&lt;service&gt;.&lt;hành động&gt;</c>.</summary>
    string Topic { get; }

    /// <summary>
    /// Làm việc của task và trả về các biến ghi ngược vào process.
    /// </summary>
    /// <remarks>
    /// Ném ngoại lệ nghĩa là thất bại — <see cref="Camunda.ExternalTaskWorker"/> sẽ báo
    /// lỗi về engine kèm số lần thử còn lại. Đừng nuốt ngoại lệ rồi trả về rỗng: khi ấy
    /// process chạy tiếp như thể bước này đã xong.
    /// </remarks>
    Task<IReadOnlyDictionary<string, CamundaVariable>?> XuLyAsync(
        ExternalTask task,
        CancellationToken huy);
}

/// <summary>
/// Thông báo cho chuyên gia rằng có việc cần review (<c>corpus.notify-expert</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>Kênh thông báo hiện tại là log có cấu trúc.</b> Đó là một kênh thật, không phải một
/// lớp giả: bản ghi đi ra stdout của container, và ở môi trường thật nó vào CloudWatch nơi
/// người vận hành đọc được. Cái nó chưa làm là **đẩy** tới người nhận.
/// </para>
/// <para>
/// Nâng cấp về sau chỉ thay **nơi gửi đến**, không thay cơ chế: thêm một lớp gửi email hoặc
/// đẩy thông báo vào giao diện Blazor, giữ nguyên phần đọc biến và phần hoàn thành task.
/// Đó là lý do đường đi qua engine, qua fetch-and-lock, qua complete được cài đặt đầy đủ
/// ngay từ bây giờ, còn phần gửi thì tối giản.
/// </para>
/// <para>
/// ⚠️ Không tự bịa ra dữ liệu nghiệp vụ. Handler này chỉ đọc biến có sẵn và ghi lại; nó
/// **không** trả về một con số "số chunk bị ảnh hưởng" nào, vì bảng chunk chưa tồn tại
/// (epic E2). Một handler trả về số liệu bịa còn tệ hơn một handler chưa làm gì.
/// </para>
/// </remarks>
/// <param name="log">Nơi thông báo đi ra.</param>
public sealed class NotifyExpertHandler(ILogger<NotifyExpertHandler> log) : IExternalTaskHandler
{
    /// <inheritdoc />
    public string Topic => "corpus.notify-expert";

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, CamundaVariable>?> XuLyAsync(
        ExternalTask task,
        CancellationToken huy)
    {
        ArgumentNullException.ThrowIfNull(task);

        // Tên biến lấy từ docs/10 §1.5. Thiếu thì ghi rõ là thiếu, không thay bằng giá trị
        // mặc định — một giá trị mặc định ở đây làm thông báo gửi tới nhầm người.
        string nguoiDuyet = task.Chuoi("nguoi_duyet") ?? "(chưa gán)";
        string vanBanId = task.Chuoi("van_ban_id") ?? "(chưa có)";

        log.LogInformation(
            "Thông báo chuyên gia: process {ProcessInstanceId} cần review văn bản {VanBanId}, người duyệt {NguoiDuyet}.",
            task.ProcessInstanceId,
            vanBanId,
            nguoiDuyet);

        // Không ghi biến nào ngược vào process: docs/10 §1.4 khai topic này có Output là "—".
        return Task.FromResult<IReadOnlyDictionary<string, CamundaVariable>?>(null);
    }
}
