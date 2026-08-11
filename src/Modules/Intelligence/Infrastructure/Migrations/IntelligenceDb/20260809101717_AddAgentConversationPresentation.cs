using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Intelligence.Infrastructure.Migrations.IntelligenceDb
{
    /// <inheritdoc />
    public partial class AddAgentConversationPresentation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LatestTextPreview",
                schema: "Intelligence",
                table: "AgentConversations",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtectedPresentationTranscript",
                schema: "Intelligence",
                table: "AgentConversations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "Intelligence",
                table: "AgentConversations",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatestTextPreview",
                schema: "Intelligence",
                table: "AgentConversations");

            migrationBuilder.DropColumn(
                name: "ProtectedPresentationTranscript",
                schema: "Intelligence",
                table: "AgentConversations");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "Intelligence",
                table: "AgentConversations");
        }
    }
}
