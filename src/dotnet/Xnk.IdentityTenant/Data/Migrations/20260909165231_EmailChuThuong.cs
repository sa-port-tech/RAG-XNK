using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xnk.IdentityTenant.Data.Migrations
{
    /// <inheritdoc />
    public partial class EmailChuThuong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_users_Email_chu_thuong",
                schema: "identity",
                table: "users",
                sql: "\"Email\" = lower(\"Email\")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_users_Email_chu_thuong",
                schema: "identity",
                table: "users");
        }
    }
}
