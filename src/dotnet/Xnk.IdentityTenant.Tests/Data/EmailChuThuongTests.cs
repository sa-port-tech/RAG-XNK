using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xnk.IdentityTenant.Data;
using Xnk.IdentityTenant.Tests.Api;

namespace Xnk.IdentityTenant.Tests.Data;

/// <summary>
/// Email luôn được lưu chữ thường — ép ở hai tầng, không phải một quy ước.
/// </summary>
/// <remarks>
/// Trước đây "lưu chữ thường" chỉ là một câu trong tài liệu của <see cref="User.Email"/>.
/// Không có gì chuẩn hoá khi ghi, không CHECK, không citext, và chỉ mục duy nhất thì phân
/// biệt hoa thường — nên <c>Admin@x.vn</c> và <c>admin@x.vn</c> cùng tồn tại được, và bản
/// viết hoa <b>không bao giờ</b> đăng nhập được vì <c>TokenEndpoints</c> hạ chữ thường
/// trước khi tra. Triệu chứng người dùng thấy là "sai mật khẩu".
/// </remarks>
[Collection(IdentityPostgresCollection.Name)]
public sealed class EmailChuThuongTests(IdentityPostgresFixture postgres)
{
    private async Task<Tenant> TaoTenantAsync(IdentityDbContext db)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Slug = $"tenant-{Guid.NewGuid():N}",
            Name = "Tenant cho test email",
            Type = "b2b_khach_hang",
        };

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant;
    }

    [Fact]
    public async Task Ghi_email_co_chu_hoa_thi_luu_xuong_thanh_chu_thuong()
    {
        await using IdentityDbContext db = postgres.CreateContext();
        Tenant tenant = await TaoTenantAsync(db);

        string hoaThuongLan = $"Ai.Do-{Guid.NewGuid():N}@NoiBo.VN";
        Guid id = Guid.NewGuid();

        db.Users.Add(new User
        {
            Id = id,
            TenantId = tenant.Id,
            Email = hoaThuongLan,
            PasswordHash = "pbkdf2_sha256$1000$bXVvaW11b2ltdW9pbXVvaQ==$YmFtYmFtYmFtYmFtYmFtYmFtYmFtYmFtYmFtYmE=",
            Role = "user",
        });
        await db.SaveChangesAsync();

        // Đọc bằng SQL thô: đi qua EF là đi qua chính bộ chuyển đổi đang được kiểm, và khi
        // ấy test sẽ xanh kể cả khi cột lưu nguyên chữ hoa.
        await using IdentityDbContext doc = postgres.CreateContext();
        string? trongBang = await doc.Database
            .SqlQuery<string>($"""SELECT "Email" AS "Value" FROM identity.users WHERE "Id" = {id}""")
            .SingleOrDefaultAsync();

        Assert.Equal(hoaThuongLan.ToLowerInvariant(), trongBang);
    }

    [Fact]
    public async Task Tra_cuu_bang_email_viet_hoa_van_tim_thay()
    {
        await using IdentityDbContext db = postgres.CreateContext();
        Tenant tenant = await TaoTenantAsync(db);

        string thuong = $"tra-cuu-{Guid.NewGuid():N}@noibo.vn";
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = thuong,
            PasswordHash = "pbkdf2_sha256$1000$bXVvaW11b2ltdW9pbXVvaQ==$YmFtYmFtYmFtYmFtYmFtYmFtYmFtYmFtYmFtYmE=",
            Role = "user",
        });
        await db.SaveChangesAsync();

        // Bộ chuyển đổi áp cho cả THAM SỐ truy vấn, nên vế phải cũng được hạ chữ thường.
        // Không có nó thì mọi lời gọi phải nhớ tự chuẩn hoá — và một chỗ quên là một tài
        // khoản không đăng nhập được.
        await using IdentityDbContext doc = postgres.CreateContext();
        User? tim = await doc.Users.SingleOrDefaultAsync(u => u.Email == thuong.ToUpperInvariant());

        Assert.NotNull(tim);
    }

    [Fact]
    public async Task Chen_thang_bang_SQL_voi_email_viet_hoa_thi_database_TU_CHOI()
    {
        // ⚠️ Vế quan trọng nhất của cặp ràng buộc này.
        //
        // Bộ chuyển đổi chỉ lo phần đi qua EF. Không phải thứ gì ghi vào bảng này cũng đi
        // qua EF: db/seed/0003 là SQL thuần, một lần import cũng vậy, một lần sửa tay cũng
        // vậy. CK_users_Email_chu_thuong là chỗ duy nhất áp được cho tất cả.
        await using IdentityDbContext db = postgres.CreateContext();
        Tenant tenant = await TaoTenantAsync(db);

        string sql = $"""
            INSERT INTO identity.users ("Id", "TenantId", "Email", "PasswordHash", "Role", "IsActive", "CreatedAt")
            VALUES ('{Guid.NewGuid()}', '{tenant.Id}', 'VIET.HOA@noibo.vn', 'bam', 'user', true, now())
            """;

        PostgresException loi = await Assert.ThrowsAsync<PostgresException>(
            () => db.Database.ExecuteSqlRawAsync(sql));

        Assert.Equal("23514", loi.SqlState); // check_violation
        Assert.Contains("CK_users_Email_chu_thuong", loi.Message, StringComparison.Ordinal);
    }
}
