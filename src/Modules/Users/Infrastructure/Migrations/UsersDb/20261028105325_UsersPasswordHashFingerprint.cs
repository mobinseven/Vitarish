using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Users.Infrastructure.Migrations.UsersDb
{
    /// <inheritdoc />
    public partial class UsersPasswordHashFingerprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "PasswordHashFingerprint",
                schema: "Users",
                table: "AspNetUsers",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHashFingerprint",
                schema: "Users",
                table: "AspNetUsers");
        }
    }
}
