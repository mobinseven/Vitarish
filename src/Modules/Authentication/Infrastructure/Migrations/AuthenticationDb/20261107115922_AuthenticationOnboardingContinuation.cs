using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Authentication.Infrastructure.Migrations.AuthenticationDb
{
    /// <inheritdoc />
    public partial class AuthenticationOnboardingContinuation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PriorCentralSessionId",
                schema: "Authentication",
                table: "TenantHandoffTickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnOrigin",
                schema: "Authentication",
                table: "TenantHandoffTickets",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriorCentralSessionId",
                schema: "Authentication",
                table: "TenantHandoffTickets");

            migrationBuilder.DropColumn(
                name: "ReturnOrigin",
                schema: "Authentication",
                table: "TenantHandoffTickets");
        }
    }
}
