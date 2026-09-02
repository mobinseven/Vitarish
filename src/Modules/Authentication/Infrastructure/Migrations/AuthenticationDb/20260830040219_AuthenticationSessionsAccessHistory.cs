using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Authentication.Infrastructure.Migrations.AuthenticationDb
{
    /// <inheritdoc />
    public partial class AuthenticationSessionsAccessHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedSessionStartIpAddresses",
                schema: "Authentication",
                table: "SecurityAccessPolicies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ConcurrentSessionsFunctionallyRequired",
                schema: "Authentication",
                table: "SecurityAccessPolicies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DefaultInactivityTimeoutMinutes",
                schema: "Authentication",
                table: "SecurityAccessPolicies",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "MaximumConcurrentSessions",
                schema: "Authentication",
                table: "SecurityAccessPolicies",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "AuthenticationAccessHistories",
                schema: "Authentication",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PreviousSuccessfulAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSuccessfulAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUnsuccessfulAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAttemptsSincePreviousSuccessful = table.Column<int>(type: "integer", nullable: false),
                    CurrentFailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationAccessHistories", x => new { x.UserId, x.TenantId });
                });

            migrationBuilder.CreateTable(
                name: "AuthenticationSessionNotices",
                schema: "Authentication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NoticeType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationSessionNotices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthenticationSessions",
                schema: "Authentication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClientIpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    TerminatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TerminationReason = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthenticationUserSessionPolicies",
                schema: "Authentication",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    InactivityTimeoutMinutes = table.Column<int>(type: "integer", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationUserSessionPolicies", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationSessionNotices_SessionId_AcknowledgedAt",
                schema: "Authentication",
                table: "AuthenticationSessionNotices",
                columns: new[] { "SessionId", "AcknowledgedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationSessions_LastActivityAt",
                schema: "Authentication",
                table: "AuthenticationSessions",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationSessions_UserId_TerminatedAt",
                schema: "Authentication",
                table: "AuthenticationSessions",
                columns: new[] { "UserId", "TerminatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthenticationAccessHistories",
                schema: "Authentication");

            migrationBuilder.DropTable(
                name: "AuthenticationSessionNotices",
                schema: "Authentication");

            migrationBuilder.DropTable(
                name: "AuthenticationSessions",
                schema: "Authentication");

            migrationBuilder.DropTable(
                name: "AuthenticationUserSessionPolicies",
                schema: "Authentication");

            migrationBuilder.DropColumn(
                name: "AllowedSessionStartIpAddresses",
                schema: "Authentication",
                table: "SecurityAccessPolicies");

            migrationBuilder.DropColumn(
                name: "ConcurrentSessionsFunctionallyRequired",
                schema: "Authentication",
                table: "SecurityAccessPolicies");

            migrationBuilder.DropColumn(
                name: "DefaultInactivityTimeoutMinutes",
                schema: "Authentication",
                table: "SecurityAccessPolicies");

            migrationBuilder.DropColumn(
                name: "MaximumConcurrentSessions",
                schema: "Authentication",
                table: "SecurityAccessPolicies");
        }
    }
}
