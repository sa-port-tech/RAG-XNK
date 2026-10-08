namespace Xnk.Corpus.Data;

/// <summary>
/// Một văn bản trong corpus.
/// </summary>
/// <remarks>
/// Đây là thực thể tối thiểu để lát cắt dọc chạy được, không phải mô hình đầy đủ. Phân rã
/// Điều/Khoản, quan hệ sửa đổi và đồ thị hiệu lực thuộc epic E2 (docs/09).
/// </remarks>
public sealed class Document
{
    /// <summary>Khoá chính.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant sở hữu văn bản, hoặc <c>null</c> nếu đây là văn bản quy phạm pháp luật
    /// dùng chung cho mọi tenant.
    /// </summary>
    /// <remarks>
    /// docs/00 §12: corpus pháp luật dùng chung để tiết kiệm chi phí index và embedding;
    /// chỉ corpus riêng của tenant (SOP nội bộ) mới cách ly theo <c>tenant_id</c>.
    /// Bộ lọc thực thi điều này nằm ở <see cref="CorpusDbContext.OnModelCreating"/>.
    /// </remarks>
    public Guid? TenantId { get; set; }

    /// <summary>Số hiệu văn bản, ví dụ "39/2018/TT-BTC".</summary>
    public required string DocumentNumber { get; set; }

    /// <summary>Trích yếu.</summary>
    public required string Title { get; set; }

    /// <summary>
    /// Ngày bắt đầu có hiệu lực.
    /// </summary>
    /// <remarks>
    /// Hai trường hiệu lực có mặt ngay từ thực thể đầu tiên là có chủ đích: Sprint Goal
    /// của Sprint 1 là chứng minh bằng SQL rằng hệ thống phân biệt đúng điều khoản
    /// còn/hết hiệu lực. Thêm chúng sau nghĩa là viết migration sửa dữ liệu đã có.
    /// </remarks>
    public DateOnly? EffectiveFrom { get; set; }

    /// <summary>Ngày hết hiệu lực. <c>null</c> nghĩa là còn hiệu lực.</summary>
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>Thời điểm bản ghi được tạo, theo UTC.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
