using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitarish.Main.Migrations.ContentsDb
{
    /// <inheritdoc />
    public partial class ContentsInitialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Contents");

            migrationBuilder.CreateTable(
                name: "Assets",
                schema: "Contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    AltText = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'{}'"),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentItems",
                schema: "Contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CanonicalPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Excerpt = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'[]'"),
                    Metadata = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'{}'"),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Locale = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Terms",
                schema: "Contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Metadata = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'{}'"),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Terms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Terms_Terms_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "Contents",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContentAssets",
                schema: "Contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentAssets_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "Contents",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentAssets_ContentItems_ContentId",
                        column: x => x.ContentId,
                        principalSchema: "Contents",
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentRelations",
                schema: "Contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(9,4)", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: false, defaultValueSql: "'{}'"),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentRelations_ContentItems_FromContentId",
                        column: x => x.FromContentId,
                        principalSchema: "Contents",
                        principalTable: "ContentItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContentRelations_ContentItems_ToContentId",
                        column: x => x.ToContentId,
                        principalSchema: "Contents",
                        principalTable: "ContentItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContentRevisions",
                schema: "Contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Snapshot = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentRevisions_ContentItems_ContentId",
                        column: x => x.ContentId,
                        principalSchema: "Contents",
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentTerms",
                schema: "Contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTerms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentTerms_ContentItems_ContentId",
                        column: x => x.ContentId,
                        principalSchema: "Contents",
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentTerms_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Contents",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_FileId",
                schema: "Contents",
                table: "Assets",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_Url",
                schema: "Contents",
                table: "Assets",
                column: "Url");

            migrationBuilder.CreateIndex(
                name: "IX_ContentAssets_AssetId",
                schema: "Contents",
                table: "ContentAssets",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentAssets_ContentId_AssetId_Role",
                schema: "Contents",
                table: "ContentAssets",
                columns: new[] { "ContentId", "AssetId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentAssets_ContentId_Role_Position",
                schema: "Contents",
                table: "ContentAssets",
                columns: new[] { "ContentId", "Role", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_AuthorId",
                schema: "Contents",
                table: "ContentItems",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_Locale",
                schema: "Contents",
                table: "ContentItems",
                column: "Locale");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_PublishedAt",
                schema: "Contents",
                table: "ContentItems",
                column: "PublishedAt");

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

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_Type_Status_PublishedAt",
                schema: "Contents",
                table: "ContentItems",
                columns: new[] { "Type", "Status", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentRelations_FromContentId_ToContentId_RelationType",
                schema: "Contents",
                table: "ContentRelations",
                columns: new[] { "FromContentId", "ToContentId", "RelationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentRelations_ToContentId_RelationType",
                schema: "Contents",
                table: "ContentRelations",
                columns: new[] { "ToContentId", "RelationType" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentRevisions_ContentId_Version",
                schema: "Contents",
                table: "ContentRevisions",
                columns: new[] { "ContentId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentTerms_ContentId_TermId_Role",
                schema: "Contents",
                table: "ContentTerms",
                columns: new[] { "ContentId", "TermId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentTerms_TermId_ContentId",
                schema: "Contents",
                table: "ContentTerms",
                columns: new[] { "TermId", "ContentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Terms_Kind_ParentId",
                schema: "Contents",
                table: "Terms",
                columns: new[] { "Kind", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Terms_ParentId",
                schema: "Contents",
                table: "Terms",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Terms_TenantId_Slug",
                schema: "Contents",
                table: "Terms",
                columns: new[] { "TenantId", "Slug" },
                unique: true,
                filter: "\"Slug\" IS NOT NULL AND \"Slug\" <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentAssets",
                schema: "Contents");

            migrationBuilder.DropTable(
                name: "ContentRelations",
                schema: "Contents");

            migrationBuilder.DropTable(
                name: "ContentRevisions",
                schema: "Contents");

            migrationBuilder.DropTable(
                name: "ContentTerms",
                schema: "Contents");

            migrationBuilder.DropTable(
                name: "Assets",
                schema: "Contents");

            migrationBuilder.DropTable(
                name: "ContentItems",
                schema: "Contents");

            migrationBuilder.DropTable(
                name: "Terms",
                schema: "Contents");
        }
    }
}
