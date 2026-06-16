using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Requests.Infrastructure.Migrations.RequestsDb
{
    /// <inheritdoc />
    public partial class RequestsInitialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Requests");

            migrationBuilder.CreateTable(
                name: "Requests",
                schema: "Requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Authority = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ResourceId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    TraceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AdditionalInfo = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Requests_Authority",
                schema: "Requests",
                table: "Requests",
                column: "Authority");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ClientId",
                schema: "Requests",
                table: "Requests",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_OwnerId",
                schema: "Requests",
                table: "Requests",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_RequestId",
                schema: "Requests",
                table: "Requests",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ResourceId",
                schema: "Requests",
                table: "Requests",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_TenantId",
                schema: "Requests",
                table: "Requests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_UserId",
                schema: "Requests",
                table: "Requests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Requests",
                schema: "Requests");
        }
    }
}
