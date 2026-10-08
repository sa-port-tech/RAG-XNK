namespace Xnk.WorkflowWorker.Camunda;

/// <summary>
/// Dấu vết cho biết vòng lặp lấy task còn sống hay không.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CamundaHealthCheck"/> trước đây chỉ chứng minh <b>gọi được Camunda</b>. Đó là
/// một câu trả lời cho câu hỏi khác: engine hoàn toàn có thể khoẻ trong khi vòng lặp của
/// worker đã dừng — và khi ấy readiness vẫn xanh, mọi bảng điều khiển vẫn xanh, còn task
/// thì nằm im trong hàng đợi.
/// </para>
/// <para>
/// Đối tượng này là thứ phân biệt hai trạng thái đó. Worker ghi mốc thời gian sau mỗi lượt;
/// health check đọc mốc ấy và hỏi "lượt gần nhất cách đây bao lâu".
/// </para>
/// <para>
/// Đăng ký <b>singleton</b>: worker và health check phải nhìn cùng một đối tượng, hai bản
/// scoped là hai câu chuyện không liên quan.
/// </para>
/// </remarks>
public sealed class NhipTimWorker(TimeProvider dongHo)
{
    private long _mocCuoiTick;

    /// <summary>Vòng lặp đã khởi động chưa.</summary>
    public bool DaBatDau { get; private set; }

    /// <summary>Thời điểm kết thúc lượt lấy task gần nhất.</summary>
    public DateTimeOffset? LuotGanNhat =>
        Interlocked.Read(ref _mocCuoiTick) is var tick && tick == 0
            ? null
            : new DateTimeOffset(tick, TimeSpan.Zero);

    /// <summary>Thời điểm hiện tại theo đồng hồ mà worker và health check dùng chung.</summary>
    /// <remarks>
    /// Lộ ra ở đây thay vì tiêm <see cref="TimeProvider"/> vào worker lần nữa: hai chỗ đọc
    /// hai nguồn thời gian là cách sinh ra những phép trừ cho ra số âm trong test.
    /// </remarks>
    public DateTimeOffset BayGio => dongHo.GetUtcNow();

    /// <summary>Worker gọi khi vòng lặp bắt đầu.</summary>
    public void BatDau()
    {
        DaBatDau = true;
        Dap();
    }

    /// <summary>Worker gọi sau mỗi lượt lấy task, kể cả lượt không có task nào.</summary>
    /// <remarks>
    /// Đập cả khi hàng đợi rỗng là có chủ đích: thứ đang đo là <b>vòng lặp còn quay</b>,
    /// không phải <b>có việc để làm</b>. Chỉ đập khi có task nghĩa là một buổi tối yên ả
    /// cũng bị chấm là hỏng.
    /// </remarks>
    public void Dap() => Interlocked.Exchange(ref _mocCuoiTick, dongHo.GetUtcNow().UtcTicks);
}
