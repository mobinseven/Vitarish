using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Intelligence.Infrastructure.Migrations.IntelligenceDb
{
    /// <inheritdoc />
    public partial class AgentConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Intelligence");

            migrationBuilder.CreateTable(
                name: "AgentConversations",
                schema: "Intelligence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatingUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AgentKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SubjectKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SubjectId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProtectedHistory = table.Column<string>(type: "text", nullable: true),
                    HistoryFormat = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    ActiveLeaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveLeaseExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentConversations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentConversations_TenantId_CreatingUserId_AgentKey_Subject~",
                schema: "Intelligence",
                table: "AgentConversations",
                columns: new[] { "TenantId", "CreatingUserId", "AgentKey", "SubjectKind", "SubjectId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentConversations",
                schema: "Intelligence");
        }
    }
}
