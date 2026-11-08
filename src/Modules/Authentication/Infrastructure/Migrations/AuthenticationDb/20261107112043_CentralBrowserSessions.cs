using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Authentication.Infrastructure.Migrations.AuthenticationDb
{
    /// <inheritdoc />
    public partial class CentralBrowserSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CentralSessionId",
                schema: "Authentication",
                table: "TenantHandoffTickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationHash",
                schema: "Authentication",
                table: "TenantHandoffTickets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnPath",
                schema: "Authentication",
                table: "TenantHandoffTickets",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CentralSessionId",
                schema: "Authentication",
                table: "AuthenticationSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuthenticationContinuation",
                schema: "Authentication",
                columns: table => new
                {
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Origin = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ReturnPath = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    CorrelationHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationContinuation", x => x.TokenHash);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationSessions_CentralSessionId",
                schema: "Authentication",
                table: "AuthenticationSessions",
                column: "CentralSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationContinuation_ExpiresAt",
                schema: "Authentication",
                table: "AuthenticationContinuation",
                column: "ExpiresAt");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthenticationSessions_AuthenticationSessions_CentralSessio~",
                schema: "Authentication",
                table: "AuthenticationSessions",
                column: "CentralSessionId",
                principalSchema: "Authentication",
                principalTable: "AuthenticationSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthenticationSessions_AuthenticationSessions_CentralSessio~",
                schema: "Authentication",
                table: "AuthenticationSessions");

            migrationBuilder.DropTable(
                name: "AuthenticationContinuation",
                schema: "Authentication");

            migrationBuilder.DropIndex(
                name: "IX_AuthenticationSessions_CentralSessionId",
                schema: "Authentication",
                table: "AuthenticationSessions");

            migrationBuilder.DropColumn(
                name: "CentralSessionId",
                schema: "Authentication",
                table: "TenantHandoffTickets");

            migrationBuilder.DropColumn(
                name: "CorrelationHash",
                schema: "Authentication",
                table: "TenantHandoffTickets");

            migrationBuilder.DropColumn(
                name: "ReturnPath",
                schema: "Authentication",
                table: "TenantHandoffTickets");

            migrationBuilder.DropColumn(
                name: "CentralSessionId",
                schema: "Authentication",
                table: "AuthenticationSessions");
        }
    }
}
