using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Parties.Infrastructure.Migrations.PartiesDb
{
    /// <inheritdoc />
    public partial class PartiesTenantSetupGuideDismissal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SetupGuideDismissedAt",
                schema: "Parties",
                table: "TenantBusinessProfiles",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SetupGuideDismissedAt",
                schema: "Parties",
                table: "TenantBusinessProfiles");
        }
    }
}
