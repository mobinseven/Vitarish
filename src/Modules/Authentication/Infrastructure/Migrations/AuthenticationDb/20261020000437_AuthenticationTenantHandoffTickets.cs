using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Authentication.Infrastructure.Migrations.AuthenticationDb
{
    /// <inheritdoc />
    public partial class AuthenticationTenantHandoffTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantHandoffTickets",
                schema: "Authentication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TargetHost = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantHandoffTickets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantHandoffTickets_TokenHash",
                schema: "Authentication",
                table: "TenantHandoffTickets",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantHandoffTickets_UserId_ExpiresAt",
                schema: "Authentication",
                table: "TenantHandoffTickets",
                columns: new[] { "UserId", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantHandoffTickets",
                schema: "Authentication");
        }
    }
}
