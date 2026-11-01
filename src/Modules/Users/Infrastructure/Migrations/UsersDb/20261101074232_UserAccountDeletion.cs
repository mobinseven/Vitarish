using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Users.Infrastructure.Migrations.UsersDb
{
    /// <inheritdoc />
    public partial class UserAccountDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "Users",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "Users",
                table: "AspNetUsers");
        }
    }
}
