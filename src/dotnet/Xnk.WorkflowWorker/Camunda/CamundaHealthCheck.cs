using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Xnk.WorkflowWorker.Camunda;

/// <summary>
/// Readiness của worker: gọi được REST API của Camunda hay không.
/// </summary>
/// <remarks>
/// <para>
/// Worker không phục vụ lưu lượng người dùng, nên "sẵn sàng" ở đây có nghĩa khác với ở
/// corpus hay chat: nó nói rằng tiến trình này **đang thật sự làm được việc của mình**.
/// Engine không gọi tới được thì worker sống mà không lấy được task nào — và một container
/// sống nhưng vô dụng là loại hỏng hóc im lặng nhất, vì mọi bảng điều khiển đều xanh.
/// </para>
/// <para>
/// Liveness thì **không** chạm Camunda: nếu có, một sự cố engine sẽ khiến ECS giết và khởi
/// động lại worker liên tục, biến sự cố phụ thuộc thành sự cố lan rộng.
/// </para>
/// </remarks>
/// <param name="client">Lớp gọi REST API.</param>
/// <param name="nhipTim">Dấu vết của vòng lặp lấy task.</param>
/// <param name="options">Tham số fetch-and-lock, dùng để tính ngưỡng im lặng.</param>
/// <param name="dongHo">Nguồn thời gian, thay được trong test.</param>
public sealed class CamundaHealthCheck(
    CamundaClient client,
    NhipTimWorker nhipTim,
    IOptions<CamundaOptions> options,
    TimeProvider dongHo) : IHealthCheck
{
    /// <summary>
    /// Hệ số nhân cho ngưỡng im lặng, so với thời gian chờ dài nhất của một lượt.
    /// </summary>
    /// <remarks>
    /// Một lượt bình thường dài nhất bằng <c>AsyncResponseTimeoutMs</c> (long polling
    /// không có task nào). Nhân ba để một lượt chậm bất thường — engine bận, mạng lag —
    /// không bị chấm là chết. Nhỏ hơn thì health check báo động giả; lớn hơn thì một
    /// worker đã đứng hình được coi là khoẻ quá lâu.
    /// </remarks>
    public const int HeSoNguongImLang = 3;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Vế 1 — engine có gọi tới được không.
        try
        {
            await client.KiemTraKetNoiAsync(cancellationToken);
        }
        catch (Exception loi) when (loi is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Không gọi được Camunda REST API.", loi);
        }

        // Vế 2 — vòng lặp của CHÍNH worker này còn quay không.
        //
        // Đây là vế mà bản trước thiếu, và là vế quan trọng hơn. Engine khoẻ hoàn toàn
        // không nói gì về việc tiến trình này còn lấy task hay đã đứng im: hai điều kiện
        // độc lập, và cái thứ hai mới là thứ readiness của một worker phải trả lời.
        if (!nhipTim.DaBatDau)
        {
            return HealthCheckResult.Unhealthy("Vòng lặp lấy task chưa khởi động.");
        }

        DateTimeOffset? luot = nhipTim.LuotGanNhat;
        if (luot is null)
        {
            return HealthCheckResult.Unhealthy("Chưa có lượt lấy task nào hoàn tất.");
        }

        TimeSpan imLang = dongHo.GetUtcNow() - luot.Value;
        TimeSpan nguong = TimeSpan.FromMilliseconds(
            (long)options.Value.AsyncResponseTimeoutMs * HeSoNguongImLang);

        if (imLang > nguong)
        {
            return HealthCheckResult.Unhealthy(
                $"Vòng lặp lấy task im lặng {imLang.TotalSeconds:F0}s, quá ngưỡng "
                + $"{nguong.TotalSeconds:F0}s — engine gọi được nhưng worker không làm việc.");
        }

        return HealthCheckResult.Healthy(
            $"Gọi được Camunda REST API; lượt lấy task gần nhất cách đây {imLang.TotalSeconds:F0}s.");
    }
}
