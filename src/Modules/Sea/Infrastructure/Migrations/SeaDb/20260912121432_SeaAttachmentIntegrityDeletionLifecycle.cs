using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Sea.Infrastructure.Migrations.SeaDb
{
    /// <inheritdoc />
    public partial class SeaAttachmentIntegrityDeletionLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttachmentIntegrityRecords_Attachments_AttachmentId",
                schema: "Sea",
                table: "AttachmentIntegrityRecords");

            migrationBuilder.AddForeignKey(
                name: "FK_AttachmentIntegrityRecords_Attachments_AttachmentId",
                schema: "Sea",
                table: "AttachmentIntegrityRecords",
                column: "AttachmentId",
                principalSchema: "Sea",
                principalTable: "Attachments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttachmentIntegrityRecords_Attachments_AttachmentId",
                schema: "Sea",
                table: "AttachmentIntegrityRecords");

            migrationBuilder.AddForeignKey(
                name: "FK_AttachmentIntegrityRecords_Attachments_AttachmentId",
                schema: "Sea",
                table: "AttachmentIntegrityRecords",
                column: "AttachmentId",
                principalSchema: "Sea",
                principalTable: "Attachments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
