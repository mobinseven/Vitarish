using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Communications.Infrastructure.Migrations.CommunicationsDb
{
    /// <inheritdoc />
    public partial class ReminderDeliveryAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryAcceptedAt",
                schema: "Communications",
                table: "Reminders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryAcceptedAt",
                schema: "Communications",
                table: "Reminders");
        }
    }
}
