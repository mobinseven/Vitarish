using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Platform.Infrastructure.Migrations.PlatformDb
{
    /// <inheritdoc />
    public partial class RemovePlatformMemoryRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectionMemorySamples",
                schema: "Platform");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConnectionMemorySamples",
                schema: "Platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CircuitId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EstimatedCircuitDeltaBytes = table.Column<long>(type: "bigint", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ManagedHeapBytes = table.Column<long>(type: "bigint", nullable: false),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessPrivateMemoryBytes = table.Column<long>(type: "bigint", nullable: false),
                    ProcessWorkingSetBytes = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
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
    }
}
