using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Intelligence.Infrastructure.Migrations.IntelligenceDb
{
    /// <inheritdoc />
    public partial class AgentProfileCompatibilityRepair : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileCompatibilityKey",
                schema: "Intelligence",
                table: "AgentConversations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileCompatibilityKey",
                schema: "Intelligence",
                table: "AgentConversations");
        }
    }
}
