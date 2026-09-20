using FluentAssertions;
using Npgsql;
using NpgsqlTypes;

namespace Vitarish.Clinic.Tests.Infrastructure;

public sealed class PatientTagsMaterializedViewTests
{
    [Fact]
    public async Task Materialized_tags_are_normalized_and_gin_queryable()
    {
        string? connectionString = Environment.GetEnvironmentVariable("CLINIC_MATERIALIZED_VIEW_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        try
        {
            await ExecuteAsync(connection, """
                CREATE SCHEMA IF NOT EXISTS "Clinic";
                DROP MATERIALIZED VIEW IF EXISTS "Clinic"."mv_PatientTagsTest";
                DROP TABLE IF EXISTS "Clinic"."PatientTagsTestSource";
                CREATE TABLE "Clinic"."PatientTagsTestSource" (
                    "Id" uuid PRIMARY KEY,
                    "Metadata" jsonb NOT NULL
                );
                INSERT INTO "Clinic"."PatientTagsTestSource" ("Id", "Metadata") VALUES
                    ('00000000-0000-0000-0000-000000000001', '{"Tags":[" افزایش قد ","افزایش قد","follow-up","Follow-Up"]}'),
                    ('00000000-0000-0000-0000-000000000002', '{"Tags":null}'),
                    ('00000000-0000-0000-0000-000000000003', '{"Tags":"not-an-array"}');
                CREATE MATERIALIZED VIEW "Clinic"."mv_PatientTagsTest" AS
                SELECT
                    "Id",
                    CASE
                        WHEN jsonb_typeof("Metadata" -> 'Tags') = 'array' THEN COALESCE(
                    ARRAY(
                        SELECT tag
                        FROM (
                            SELECT DISTINCT ON (lower(btrim(value)))
                                btrim(value) AS tag,
                                position
                            FROM jsonb_array_elements_text("Metadata" -> 'Tags') WITH ORDINALITY AS value(value, position)
                            WHERE btrim(value) <> ''
                            ORDER BY lower(btrim(value)), position
                        ) normalized
                        ORDER BY position
                    ),
                            ARRAY[]::text[]
                        )
                        ELSE ARRAY[]::text[]
                    END AS "Tags"
                FROM "Clinic"."PatientTagsTestSource";
                CREATE UNIQUE INDEX "IX_mv_PatientTagsTest_Id"
                    ON "Clinic"."mv_PatientTagsTest" ("Id");
                CREATE INDEX "IX_mv_PatientTagsTest_Tags"
                    ON "Clinic"."mv_PatientTagsTest" USING GIN ("Tags");
                """);

            string[] tags = ["افزایش قد", "follow-up"];
            await using (NpgsqlCommand command = new(
                "SELECT \"Id\" FROM \"Clinic\".\"mv_PatientTagsTest\" WHERE \"Tags\" @> @tags;",
                connection))
            {
                command.Parameters.Add(new NpgsqlParameter<string[]>("tags", tags)
                {
                    NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text
                });

                (await command.ExecuteScalarAsync()).Should().Be(Guid.Parse("00000000-0000-0000-0000-000000000001"));
            }

            await ExecuteAsync(connection, "SET enable_seqscan = off;");
            string queryPlan = await ExplainAsync(connection, tags);
            queryPlan.Should().Contain("Bitmap Index Scan").And.Contain("IX_mv_PatientTagsTest_Tags");

            object? emptyTagRows = await ScalarAsync(connection, "SELECT count(*) FROM \"Clinic\".\"mv_PatientTagsTest\" WHERE cardinality(\"Tags\") = 0;");
            Convert.ToInt64(emptyTagRows).Should().Be(2);

            object? normalizedTags = await ScalarAsync(connection, "SELECT \"Tags\" FROM \"Clinic\".\"mv_PatientTagsTest\" WHERE \"Id\" = '00000000-0000-0000-0000-000000000001';");
            ((string[])normalizedTags!).Should().Equal("افزایش قد", "follow-up");

            object? indexCount = await ScalarAsync(connection, "SELECT count(*) FROM pg_indexes WHERE schemaname = 'Clinic' AND indexname = 'IX_mv_PatientTagsTest_Tags';");
            Convert.ToInt64(indexCount).Should().Be(1);

            object? refreshIndexCount = await ScalarAsync(connection, "SELECT count(*) FROM pg_indexes WHERE schemaname = 'Clinic' AND indexname = 'IX_mv_PatientTagsTest_Id';");
            Convert.ToInt64(refreshIndexCount).Should().Be(1);
        }
        finally
        {
            await ExecuteAsync(connection, "DROP MATERIALIZED VIEW IF EXISTS \"Clinic\".\"mv_PatientTagsTest\"; DROP TABLE IF EXISTS \"Clinic\".\"PatientTagsTestSource\";");
        }
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using NpgsqlCommand command = new(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<object?> ScalarAsync(NpgsqlConnection connection, string sql)
    {
        await using NpgsqlCommand command = new(sql, connection);
        return await command.ExecuteScalarAsync();
    }

    private static async Task<string> ExplainAsync(NpgsqlConnection connection, string[] tags)
    {
        await using NpgsqlCommand command = new(
            "EXPLAIN (COSTS OFF) SELECT \"Id\" FROM \"Clinic\".\"mv_PatientTagsTest\" WHERE \"Tags\" @> @tags;",
            connection);
        command.Parameters.Add(new NpgsqlParameter<string[]>("tags", tags)
        {
            NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text
        });

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        List<string> lines = [];
        while (await reader.ReadAsync())
        {
            lines.Add(reader.GetString(0));
        }

        return string.Join(Environment.NewLine, lines);
    }
}
