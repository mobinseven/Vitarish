using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Vitarish.Sea.Infrastructure.Data;

#nullable disable

namespace Vitarish.Sea.Infrastructure.Migrations.SeaDb
{
    /// <summary>Repairs legacy attachment metadata without changing stored attachment content.</summary>
    [DbContext(typeof(SeaDbContext))]
    [Migration("20260811113000_SeaAttachmentLegacyMetadataRepair")]
    public sealed class SeaAttachmentLegacyMetadataRepair : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH "ClassifiedAttachments" AS (
                    SELECT
                        "Id",
                        CASE
                            WHEN "Kind" = 1 THEN 1
                            WHEN lower(coalesce("ContentType", '')) LIKE 'text/%' THEN 0
                            ELSE 1
                        END AS "TargetKind",
                        CASE
                            WHEN "FileName" IS NOT NULL
                                AND btrim("FileName") <> ''
                                AND char_length("FileName") <= 255
                                AND position('/' in "FileName") = 0
                                AND position(chr(92) in "FileName") = 0
                                AND "FileName" !~ '[[:cntrl:]]'
                                THEN "FileName"
                            ELSE 'attachment-' || lower("Id"::text) || '.bin'
                        END AS "CandidateFileName"
                    FROM "Sea"."Attachments"
                ),
                "NormalizedAttachments" AS (
                    SELECT
                        "Id",
                        "TargetKind",
                        CASE WHEN "TargetKind" = 0 THEN NULL ELSE "CandidateFileName" END AS "TargetFileName"
                    FROM "ClassifiedAttachments"
                )
                UPDATE "Sea"."Attachments" AS "Attachments"
                SET "Kind" = "NormalizedAttachments"."TargetKind",
                    "FileName" = "NormalizedAttachments"."TargetFileName"
                FROM "NormalizedAttachments"
                WHERE "Attachments"."Id" = "NormalizedAttachments"."Id"
                    AND ("Attachments"."Kind", "Attachments"."FileName") IS DISTINCT FROM
                        ("NormalizedAttachments"."TargetKind", "NormalizedAttachments"."TargetFileName");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The prior schema is compatible with normalized metadata; the original ambiguous values cannot be recovered.
        }
    }
}
