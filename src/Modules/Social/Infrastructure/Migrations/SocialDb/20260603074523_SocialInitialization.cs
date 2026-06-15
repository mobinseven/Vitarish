using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Social.Infrastructure.Migrations.SocialDb
{
    /// <inheritdoc />
    public partial class SocialInitialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Social");

            migrationBuilder.CreateTable(
                name: "SocialConnections",
                schema: "Social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalAccountId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    WebhookSecret = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    WebhookVerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Metadata = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'{}'"),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RawWebhookEvents",
                schema: "Social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RawHeaders = table.Column<string>(type: "text", nullable: false),
                    RawBody = table.Column<string>(type: "text", nullable: false),
                    VerificationStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProcessingStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProcessingError = table.Column<string>(type: "text", nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawWebhookEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RawWebhookEvents_SocialConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalSchema: "Social",
                        principalTable: "SocialConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SocialConnectionTokens",
                schema: "Social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Scopes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    LastRefreshedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialConnectionTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialConnectionTokens_SocialConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalSchema: "Social",
                        principalTable: "SocialConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocialEvents",
                schema: "Social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NormalizedPayload = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialEvents_RawWebhookEvents_RawEventId",
                        column: x => x.RawEventId,
                        principalSchema: "Social",
                        principalTable: "RawWebhookEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SocialEvents_SocialConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalSchema: "Social",
                        principalTable: "SocialConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RawWebhookEvents_ConnectionId",
                schema: "Social",
                table: "RawWebhookEvents",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RawWebhookEvents_Provider_ReceivedAt",
                schema: "Social",
                table: "RawWebhookEvents",
                columns: new[] { "Provider", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RawWebhookEvents_TenantId_IdempotencyKey",
                schema: "Social",
                table: "RawWebhookEvents",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialConnections_TenantId_Provider_ExternalAccountId",
                schema: "Social",
                table: "SocialConnections",
                columns: new[] { "TenantId", "Provider", "ExternalAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialConnectionTokens_ConnectionId",
                schema: "Social",
                table: "SocialConnectionTokens",
                column: "ConnectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialEvents_ConnectionId_EventType_OccurredAt",
                schema: "Social",
                table: "SocialEvents",
                columns: new[] { "ConnectionId", "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SocialEvents_RawEventId",
                schema: "Social",
                table: "SocialEvents",
                column: "RawEventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SocialConnectionTokens",
                schema: "Social");

            migrationBuilder.DropTable(
                name: "SocialEvents",
                schema: "Social");

            migrationBuilder.DropTable(
                name: "RawWebhookEvents",
                schema: "Social");

            migrationBuilder.DropTable(
                name: "SocialConnections",
                schema: "Social");
        }
    }
}
