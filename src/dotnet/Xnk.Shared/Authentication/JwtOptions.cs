using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Xnk.Shared.Authentication;

/// <summary>
/// Tham số xác thực JWT, đọc từ section <c>Jwt</c> của cấu hình.
/// </summary>
/// <remarks>
/// Prototype dùng JWT đối xứng cho cả ba loại tenant; production chuyển sang SSO
/// SAML/OIDC nhưng giữ nguyên mô hình <c>tenant_id</c> (docs/00 §12).
/// <para>
/// <see cref="SigningKey"/> KHÔNG được có giá trị mặc định trong mã nguồn. Một khoá mặc
/// định là khoá sẽ đi thẳng lên production vào một ngày nào đó, và không ai nhớ nó tồn
/// tại. Thiếu cấu hình thì service phải chết lúc khởi động — xem
/// <see cref="JwtAuthenticationExtensions"/>.
/// </para>
/// </remarks>
public sealed class JwtOptions : IValidatableObject
{
    /// <summary>Tên section trong appsettings.</summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Độ dài tối thiểu của khoá ký, tính bằng <b>byte</b> — không phải ký tự.
    /// </summary>
    /// <remarks>
    /// HMAC-SHA256 cần khoá ít nhất bằng độ dài khối băm, tức 32 byte. Ràng buộc cũ đếm
    /// ký tự UTF-16 nên vô tình rộng hơn thực tế cần (UTF-8 không bao giờ cho ra ít byte
    /// hơn số ký tự ASCII), nhưng sai đơn vị vẫn là sai đơn vị: chỉ cần ai đó đặt khoá
    /// bằng chữ có dấu là hai phía .NET và Python bắt đầu đếm ra hai con số. Đếm byte ở
    /// cả hai — phía Python là <c>DO_DAI_KHOA_TOI_THIEU</c> trong <c>retrieval/config.py</c>.
    /// </remarks>
    public const int SoByteToiThieu = 32;

    /// <summary>
    /// Những khoá đã từng nằm trong repo, và vì thế không còn là bí mật với ai nữa.
    /// </summary>
    /// <remarks>
    /// Đây không phải danh sách "khoá yếu" — nó dài 51 ký tự và qua mọi phép đo độ dài.
    /// Vấn đề là nó được commit vào git, nên bất kỳ ai đọc lịch sử repo đều ký được token
    /// hợp lệ cho mọi service. Chặn đích danh vì một placeholder <i>dùng được</i> thì
    /// không có gì buộc ai phải thay: cách duy nhất để bước "sinh khoá của riêng bạn" trở
    /// thành bắt buộc là làm cho bước bỏ qua nó thất bại.
    /// </remarks>
    internal static readonly string[] KhoaDaCongKhai =
    [
        "khoa-ky-chi-dung-cho-may-local-khong-phai-bi-mat-32",
    ];

    /// <summary>Bên phát hành token — identity-tenant.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Đối tượng nhận token.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Khoá ký đối xứng. Lấy từ Secrets Manager khi chạy thật, từ biến môi trường khi
    /// chạy local. Tối thiểu <see cref="SoByteToiThieu"/> byte cho HMAC-SHA256.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string SigningKey { get; init; } = string.Empty;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(SigningKey))
        {
            // [Required] đã báo rồi; báo thêm ở đây chỉ làm nhiễu thông điệp khởi động.
            yield break;
        }

        int soByte = Encoding.UTF8.GetByteCount(SigningKey);
        if (soByte < SoByteToiThieu)
        {
            yield return new ValidationResult(
                $"Khoá ký JWT phải dài tối thiểu {SoByteToiThieu} byte cho HMAC-SHA256, "
                    + $"hiện có {soByte}. Sinh khoá bằng: bash tools/local/sinh-khoa.sh",
                [nameof(SigningKey)]);
        }

        if (Array.Exists(KhoaDaCongKhai, k => string.Equals(k, SigningKey, StringComparison.Ordinal)))
        {
            yield return new ValidationResult(
                "Khoá ký JWT đang dùng là khoá đã từng được commit vào repo, nên ai đọc "
                    + "được lịch sử git cũng ký được token hợp lệ cho mọi service. Sinh khoá "
                    + "riêng bằng: bash tools/local/sinh-khoa.sh",
                [nameof(SigningKey)]);
        }
    }
}
