using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Host.Migrations.RequestDb
{
    /// <inheritdoc />
    public partial class Initialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.CreateTable(
                name: "Requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Authority = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ResourceId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    TraceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AdditionalInfo = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_Requests", x => x.Id);
                });

            _ = migrationBuilder.CreateIndex(
                name: "IX_Requests_Authority",
                table: "Requests",
                column: "Authority");

            _ = migrationBuilder.CreateIndex(
                name: "IX_Requests_ClientId",
                table: "Requests",
                column: "ClientId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_Requests_OwnerId",
                table: "Requests",
                column: "OwnerId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_Requests_RequestId",
                table: "Requests",
                column: "RequestId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_Requests_ResourceId",
                table: "Requests",
                column: "ResourceId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_Requests_TenantId",
                table: "Requests",
                column: "TenantId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_Requests_UserId",
                table: "Requests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(
                name: "Requests");
    }
}
