using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Xnk.Chat.Health;

/// <summary>
/// Báo cáo trạng thái của một service phía sau — <b>không</b> gate readiness vào nó.
/// </summary>
/// <remarks>
/// <para>
/// Đọc kỹ chỗ này trước khi "sửa" nó thành <see cref="HealthStatus.Unhealthy"/>: kết quả
/// xấu nhất mà check này trả về là <see cref="HealthStatus.Degraded"/>, và
/// <c>Degraded</c> vẫn cho ra <b>HTTP 200</b>. Đó là chủ đích.
/// </para>
/// <para>
/// Chat báo not-ready khi generation chết thì ALB rút chat khỏi vòng phục vụ, và người
/// dùng nhận lỗi mạng thay vì một 502 nói rõ chuyện gì đang xảy ra. Tệ hơn: hai service
/// kiểm chéo nhau là công thức để một sự cố nhỏ hạ cả cụm. Chat vẫn phục vụ được khi phụ
/// thuộc chết — nó trả 502 kèm lý do, và đó là hành vi đúng.
/// </para>
/// <para>
/// Vậy check này để làm gì? Để <c>/chat/health/ready</c> thôi nói dối. Trước đây chat gọi
/// <c>AddHealthChecks()</c> mà không đăng ký check nào, nên readiness trả 200 vô điều kiện
/// — nó là liveness với một URL dài hơn, trong khi <c>smoke_test.sh</c> đọc đúng mã đó như
/// bằng chứng deploy thành công. Nay thân phản hồi nêu đích danh phụ thuộc nào đang hỏng,
/// mà không đổi quyết định về việc ai bị rút khỏi vòng phục vụ.
/// </para>
/// </remarks>
/// <param name="httpClientFactory">Nguồn client đã cấu hình sẵn.</param>
/// <param name="tenClient">Tên client trong <c>TenClient</c>.</param>
/// <param name="duongDanSanSang">Đường dẫn healthcheck của service phía sau.</param>
public sealed class DownstreamHealthCheck(
    IHttpClientFactory httpClientFactory,
    string tenClient,
    string duongDanSanSang) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        HttpClient client = httpClientFactory.CreateClient(tenClient);

        try
        {
            using HttpResponseMessage phanHoi = await client.GetAsync(
                new Uri(duongDanSanSang, UriKind.Relative), cancellationToken);

            return phanHoi.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"{tenClient} trả {(int)phanHoi.StatusCode}.")
                : HealthCheckResult.Degraded(
                    $"{tenClient} trả {(int)phanHoi.StatusCode} — chat vẫn nhận lưu lượng và "
                    + "sẽ trả 502 kèm lý do cho câu hỏi chạm tới nó.");
        }
        catch (Exception loi) when (loi is HttpRequestException or TaskCanceledException)
        {
            return HealthCheckResult.Degraded(
                $"Không gọi được {tenClient} — chat vẫn nhận lưu lượng và sẽ trả 502 kèm lý do.",
                loi);
        }
    }
}
