using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Main.Migrations.PlatformDb
{
    /// <inheritdoc />
    public partial class PlatformMemoryRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConnectionMemorySamples",
                schema: "Platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CircuitId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ClientId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ProcessPrivateMemoryBytes = table.Column<long>(type: "bigint", nullable: false),
                    ProcessWorkingSetBytes = table.Column<long>(type: "bigint", nullable: false),
                    ManagedHeapBytes = table.Column<long>(type: "bigint", nullable: false),
                    EstimatedCircuitDeltaBytes = table.Column<long>(type: "bigint", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionMemorySamples", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionMemorySamples_CircuitId",
                schema: "Platform",
                table: "ConnectionMemorySamples",
                column: "CircuitId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionMemorySamples_ObservedAtUtc",
                schema: "Platform",
                table: "ConnectionMemorySamples",
                column: "ObservedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectionMemorySamples",
                schema: "Platform");
        }
    }
}
