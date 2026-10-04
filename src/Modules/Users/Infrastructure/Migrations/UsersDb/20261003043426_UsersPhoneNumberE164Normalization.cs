using Microsoft.EntityFrameworkCore.Migrations;
using Vitarish.Users.Infrastructure.Data;

#nullable disable

namespace Vitarish.Users.Infrastructure.Migrations.UsersDb
{
    /// <inheritdoc />
    public partial class UsersPhoneNumberE164Normalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(PhoneNumberNormalizationSql.FunctionDefinition);

            migrationBuilder.Sql("""
                CREATE TEMP TABLE vitarish_phone_normalization AS
                SELECT "Id", "PhoneNumber" AS "Original", pg_temp.vitarish_normalize_phone("PhoneNumber") AS "Normalized"
                FROM "Users"."AspNetUsers"
                WHERE "PhoneNumber" IS NOT NULL;

                DO $$
                DECLARE
                    duplicate_ids text;
                BEGIN
                    SELECT string_agg("Id", ', ' ORDER BY "Id")
                    INTO duplicate_ids
                    FROM vitarish_phone_normalization
                    WHERE "Normalized" IS NOT NULL
                      AND "Normalized" IN (
                          SELECT "Normalized"
                          FROM vitarish_phone_normalization
                          WHERE "Normalized" IS NOT NULL
                          GROUP BY "Normalized"
                          HAVING COUNT(*) > 1);

                    IF duplicate_ids IS NOT NULL THEN
                        RAISE EXCEPTION 'Phone numbers collide after normalization. Resolve these account IDs first: %', duplicate_ids;
                    END IF;
                END $$;

                UPDATE "Users"."AspNetUsers" AS users
                SET "PhoneNumber" = normalization."Normalized",
                    "ConcurrencyStamp" = gen_random_uuid()::text
                FROM vitarish_phone_normalization AS normalization
                WHERE users."Id" = normalization."Id"
                  AND normalization."Normalized" IS NOT NULL
                  AND normalization."Normalized" <> normalization."Original";

                DO $$
                DECLARE
                    cleared_ids text;
                BEGIN
                    SELECT string_agg("Id", ', ' ORDER BY "Id")
                    INTO cleared_ids
                    FROM vitarish_phone_normalization
                    WHERE "Normalized" IS NULL;

                    IF cleared_ids IS NOT NULL THEN
                        RAISE NOTICE 'Cleared unconvertible phone numbers. Account IDs: %', cleared_ids;
                    END IF;
                END $$;

                UPDATE "Users"."AspNetUsers" AS users
                SET "PhoneNumber" = NULL,
                    "PhoneNumberConfirmed" = FALSE,
                    "ConcurrencyStamp" = gen_random_uuid()::text
                FROM vitarish_phone_normalization AS normalization
                WHERE users."Id" = normalization."Id"
                  AND normalization."Normalized" IS NULL;

                DROP TABLE vitarish_phone_normalization;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Forward-only by design: converted numbers cannot be restored to
            // their previous free-form representation. Rolling the code back is
            // safe because the old code validates E.164 and the data is E.164.
        }
    }
}
