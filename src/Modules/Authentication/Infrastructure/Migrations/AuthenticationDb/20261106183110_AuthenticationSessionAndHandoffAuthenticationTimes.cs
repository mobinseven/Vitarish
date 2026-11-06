using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Authentication.Infrastructure.Migrations.AuthenticationDb
{
    /// <inheritdoc />
    public partial class AuthenticationSessionAndHandoffAuthenticationTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AuthenticatedAt",
                schema: "Authentication",
                table: "TenantHandoffTickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AuthenticatedAt",
                schema: "Authentication",
                table: "AuthenticationSessions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthenticatedAt",
                schema: "Authentication",
                table: "TenantHandoffTickets");

            migrationBuilder.DropColumn(
                name: "AuthenticatedAt",
                schema: "Authentication",
                table: "AuthenticationSessions");
        }
    }
}
