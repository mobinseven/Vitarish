using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Contents.Infrastructure.Migrations.ContentsDb
{
    /// <inheritdoc />
    public partial class ContentsStructuredContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContentItems_TenantId_CanonicalPath",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "IX_ContentItems_TenantId_Slug",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.AddColumn<string>(
                name: "CanonicalPath",
                schema: "Contents",
                table: "ContentRevisions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Excerpt",
                schema: "Contents",
                table: "ContentRevisions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fields",
                schema: "Contents",
                table: "ContentRevisions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SchemaVersion",
                schema: "Contents",
                table: "ContentRevisions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                schema: "Contents",
                table: "ContentRevisions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "Contents",
                table: "ContentRevisions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContentTypeId",
                schema: "Contents",
                table: "ContentItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fields",
                schema: "Contents",
                table: "ContentItems",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedRevisionId",
                schema: "Contents",
                table: "ContentItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SchemaVersion",
                schema: "Contents",
                table: "ContentItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SiteId",
                schema: "Contents",
                table: "ContentItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "Contents",
                table: "ContentItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkingRevisionId",
                schema: "Contents",
                table: "ContentItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContentPublicationOutbox",
                schema: "Contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Operation = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeaseUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPublicationOutbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentTypes",
                schema: "Contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CurrentSchemaVersion = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_ContentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                schema: "Contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DefaultLocale = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Locales = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_Sites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentTypeSchemas",
                schema: "Contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Fields = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_ContentTypeSchemas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentTypeSchemas_ContentTypes_ContentTypeId",
                        column: x => x.ContentTypeId,
                        principalSchema: "Contents",
                        principalTable: "ContentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_ContentTypeId",
                schema: "Contents",
                table: "ContentItems",
                column: "ContentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_PublishedRevisionId",
                schema: "Contents",
                table: "ContentItems",
                column: "PublishedRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_SiteId",
                schema: "Contents",
                table: "ContentItems",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_TenantId_CanonicalPath",
                schema: "Contents",
                table: "ContentItems",
                columns: new[] { "TenantId", "CanonicalPath" },
                unique: true,
                filter: "\"SiteId\" IS NULL AND \"CanonicalPath\" IS NOT NULL AND \"CanonicalPath\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_TenantId_SiteId_CanonicalPath",
                schema: "Contents",
                table: "ContentItems",
                columns: new[] { "TenantId", "SiteId", "CanonicalPath" },
                unique: true,
                filter: "\"SiteId\" IS NOT NULL AND \"CanonicalPath\" IS NOT NULL AND \"CanonicalPath\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_TenantId_Slug",
                schema: "Contents",
                table: "ContentItems",
                columns: new[] { "TenantId", "Slug" },
                unique: true,
                filter: "\"SiteId\" IS NULL AND \"Slug\" IS NOT NULL AND \"Slug\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_WorkingRevisionId",
                schema: "Contents",
                table: "ContentItems",
                column: "WorkingRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPublicationOutbox_TenantId_CompletedAt_NextAttemptAt~",
                schema: "Contents",
                table: "ContentPublicationOutbox",
                columns: new[] { "TenantId", "CompletedAt", "NextAttemptAt", "LeaseUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_TenantId_Key",
                schema: "Contents",
                table: "ContentTypes",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeSchemas_ContentTypeId_Version",
                schema: "Contents",
                table: "ContentTypeSchemas",
                columns: new[] { "ContentTypeId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sites_TenantId_Key",
                schema: "Contents",
                table: "Sites",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentItems_ContentRevisions_PublishedRevisionId",
                schema: "Contents",
                table: "ContentItems",
                column: "PublishedRevisionId",
                principalSchema: "Contents",
                principalTable: "ContentRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentItems_ContentRevisions_WorkingRevisionId",
                schema: "Contents",
                table: "ContentItems",
                column: "WorkingRevisionId",
                principalSchema: "Contents",
                principalTable: "ContentRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentItems_ContentTypes_ContentTypeId",
                schema: "Contents",
                table: "ContentItems",
                column: "ContentTypeId",
                principalSchema: "Contents",
                principalTable: "ContentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentItems_Sites_SiteId",
                schema: "Contents",
                table: "ContentItems",
                column: "SiteId",
                principalSchema: "Contents",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContentItems_ContentRevisions_PublishedRevisionId",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ContentItems_ContentRevisions_WorkingRevisionId",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ContentItems_ContentTypes_ContentTypeId",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ContentItems_Sites_SiteId",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.DropTable(
                name: "ContentPublicationOutbox",
                schema: "Contents");

            migrationBuilder.DropTable(
                name: "ContentTypeSchemas",
                schema: "Contents");

            migrationBuilder.DropTable(
                name: "Sites",
                schema: "Contents");

            migrationBuilder.DropTable(
                name: "ContentTypes",
                schema: "Contents");

            migrationBuilder.DropIndex(
                name: "IX_ContentItems_ContentTypeId",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "IX_ContentItems_PublishedRevisionId",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "IX_ContentItems_SiteId",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "IX_ContentItems_TenantId_CanonicalPath",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "IX_ContentItems_TenantId_SiteId_CanonicalPath",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "IX_ContentItems_TenantId_Slug",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.DropIndex(
                name: "IX_ContentItems_WorkingRevisionId",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "CanonicalPath",
                schema: "Contents",
                table: "ContentRevisions");

            migrationBuilder.DropColumn(
                name: "Excerpt",
                schema: "Contents",
                table: "ContentRevisions");

            migrationBuilder.DropColumn(
                name: "Fields",
                schema: "Contents",
                table: "ContentRevisions");

            migrationBuilder.DropColumn(
                name: "SchemaVersion",
                schema: "Contents",
                table: "ContentRevisions");

            migrationBuilder.DropColumn(
                name: "Slug",
                schema: "Contents",
                table: "ContentRevisions");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "Contents",
                table: "ContentRevisions");

            migrationBuilder.DropColumn(
                name: "ContentTypeId",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "Fields",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "PublishedRevisionId",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "SchemaVersion",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "SiteId",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "WorkingRevisionId",
                schema: "Contents",
                table: "ContentItems");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_TenantId_CanonicalPath",
                schema: "Contents",
                table: "ContentItems",
                columns: new[] { "TenantId", "CanonicalPath" },
                unique: true,
                filter: "\"CanonicalPath\" IS NOT NULL AND \"CanonicalPath\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_TenantId_Slug",
                schema: "Contents",
                table: "ContentItems",
                columns: new[] { "TenantId", "Slug" },
                unique: true,
                filter: "\"Slug\" IS NOT NULL AND \"Slug\" <> ''");
        }
    }
}
