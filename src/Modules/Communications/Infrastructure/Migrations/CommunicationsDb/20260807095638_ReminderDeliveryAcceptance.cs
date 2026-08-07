using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Communications.Infrastructure.Migrations.CommunicationsDb
{
    /// <inheritdoc />
    public partial class ReminderDeliveryAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderMessageId",
                schema: "Communications",
                table: "Reminders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProviderSubmittedAt",
                schema: "Communications",
                table: "Reminders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceKind",
                schema: "Communications",
                table: "Reminders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TemplatePurpose",
                schema: "Communications",
                table: "Reminders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TemplateVariablesJson",
                schema: "Communications",
                table: "Reminders",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReminderDeliveryAttempts",
                schema: "Communications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReminderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    ProviderMessageId = table.Column<string>(type: "text", nullable: true),
                    Failure = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderDeliveryAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReminderDeliveryAttempts_Reminders_ReminderId",
                        column: x => x.ReminderId,
                        principalSchema: "Communications",
                        principalTable: "Reminders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReminderDeliveryAttempts_ReminderId_AttemptedAt",
                schema: "Communications",
                table: "ReminderDeliveryAttempts",
                columns: new[] { "ReminderId", "AttemptedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReminderDeliveryAttempts",
                schema: "Communications");

            migrationBuilder.DropColumn(
                name: "ProviderMessageId",
                schema: "Communications",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "ProviderSubmittedAt",
                schema: "Communications",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "SourceKind",
                schema: "Communications",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "TemplatePurpose",
                schema: "Communications",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "TemplateVariablesJson",
                schema: "Communications",
                table: "Reminders");
        }
    }
}
