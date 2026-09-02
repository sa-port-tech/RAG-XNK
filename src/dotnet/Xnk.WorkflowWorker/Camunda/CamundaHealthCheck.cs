using Microsoft.Extensions.Diagnostics.HealthChecks;

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
public sealed class CamundaHealthCheck(CamundaClient client) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await client.KiemTraKetNoiAsync(cancellationToken);
            return HealthCheckResult.Healthy("Gọi được Camunda REST API.");
        }
        catch (Exception loi) when (loi is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Không gọi được Camunda REST API.", loi);
        }
    }
}
