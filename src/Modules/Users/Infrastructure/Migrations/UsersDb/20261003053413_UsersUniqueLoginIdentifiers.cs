using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Users.Infrastructure.Migrations.UsersDb
{
    /// <inheritdoc />
    public partial class UsersUniqueLoginIdentifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                schema: "Users",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "Users",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true,
                filter: "\"NormalizedEmail\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "PhoneNumberIndex",
                schema: "Users",
                table: "AspNetUsers",
                column: "PhoneNumber",
                unique: true,
                filter: "\"PhoneNumber\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                schema: "Users",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "PhoneNumberIndex",
                schema: "Users",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "Users",
                table: "AspNetUsers",
                column: "NormalizedEmail");
        }
    }
}
