using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Main.Migrations.UsersDb
{
    /// <inheritdoc />
    public partial class AutoAuthLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemporaryAutoAuthLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DestinationPath = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByDeviceHint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ConsumedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    ConsumedByUserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemporaryAutoAuthLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemporaryAutoAuthLinks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryAutoAuthLinks_CodeHash",
                table: "TemporaryAutoAuthLinks",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryAutoAuthLinks_ConsumedAt",
                table: "TemporaryAutoAuthLinks",
                column: "ConsumedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryAutoAuthLinks_ExpiresAt",
                table: "TemporaryAutoAuthLinks",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryAutoAuthLinks_UserId",
                table: "TemporaryAutoAuthLinks",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemporaryAutoAuthLinks");
        }
    }
}
