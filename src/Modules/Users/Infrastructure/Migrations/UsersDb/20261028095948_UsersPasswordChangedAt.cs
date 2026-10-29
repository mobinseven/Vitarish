using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Users.Infrastructure.Migrations.UsersDb
{
    /// <inheritdoc />
    public partial class UsersPasswordChangedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordChangedAt",
                schema: "Users",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordChangedAt",
                schema: "Users",
                table: "AspNetUsers");
        }
    }
}
