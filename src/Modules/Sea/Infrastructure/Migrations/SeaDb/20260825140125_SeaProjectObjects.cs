using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Sea.Infrastructure.Migrations.SeaDb
{
    /// <inheritdoc />
    public partial class SeaProjectObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectObjects",
                schema: "Sea",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerEntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OwnerEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Data = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
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
                    table.PrimaryKey("PK_ProjectObjects", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectObjects_TenantId_ObjectType_OwnerEntityId",
                schema: "Sea",
                table: "ProjectObjects",
                columns: new[] { "TenantId", "ObjectType", "OwnerEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectObjects_TenantId_OwnerEntityType_OwnerEntityId_Objec~",
                schema: "Sea",
                table: "ProjectObjects",
                columns: new[] { "TenantId", "OwnerEntityType", "OwnerEntityId", "ObjectType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectObjects",
                schema: "Sea");
        }
    }
}
