using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Platform.Infrastructure.Migrations.PlatformDb
{
    /// <inheritdoc />
    public partial class PlatformAddLocaleNotificationRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocaleNotificationRecords",
                schema: "Platform",
                columns: table => new
                {
                    NormalizedLocale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    VerificationSmsTemplateId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SeaReminderEmailSubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SeaReminderEmailTextBody = table.Column<string>(type: "text", nullable: false),
                    SeaReminderEmailHtmlBody = table.Column<string>(type: "text", nullable: false),
                    SeaReminderSmsTemplateId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AppointmentReminderEmailSubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AppointmentReminderEmailTextBody = table.Column<string>(type: "text", nullable: false),
                    AppointmentReminderEmailHtmlBody = table.Column<string>(type: "text", nullable: false),
                    AppointmentReminderSmsTemplateId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocaleNotificationRecords", x => x.NormalizedLocale);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocaleNotificationRecords",
                schema: "Platform");
        }
    }
}
