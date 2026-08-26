using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Clinic.Infrastructure.Handlers;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Clinic.Tests.Infrastructure;

public sealed class SurgeryMaterializedViewRefresherTests
{
    [Fact]
    public async Task Refreshes_the_surgery_view_concurrently_after_source_changes()
    {
        // This connection must point to a disposable, dedicated PostgreSQL database.
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
                DROP MATERIALIZED VIEW IF EXISTS "Clinic"."mv_PatientSurgeries";
                DROP TABLE IF EXISTS "Clinic"."SurgeryRefreshSource";
                CREATE TABLE "Clinic"."SurgeryRefreshSource" ("Id" uuid PRIMARY KEY, "TenantId" text NOT NULL);
                INSERT INTO "Clinic"."SurgeryRefreshSource" ("Id", "TenantId") VALUES ('00000000-0000-0000-0000-000000000001', 'clinic-test');
                CREATE MATERIALIZED VIEW "Clinic"."mv_PatientSurgeries" AS
                    SELECT "Id", "TenantId" FROM "Clinic"."SurgeryRefreshSource";
                CREATE UNIQUE INDEX "IX_TestSurgeryRefresh_Id" ON "Clinic"."mv_PatientSurgeries" ("Id");
                """);

            DbContextOptions<ClinicDbContext> options = new DbContextOptionsBuilder<ClinicDbContext>()
                .UseNpgsql(connectionString)
                .Options;
            await using (ClinicDbContext context = new(options, new TestRequestContext(), new HttpContextAccessor()))
            {
                await ExecuteAsync(connection, "INSERT INTO \"Clinic\".\"SurgeryRefreshSource\" (\"Id\", \"TenantId\") VALUES ('00000000-0000-0000-0000-000000000002', 'clinic-test');");

                await new SurgeryMaterializedViewRefresher(context).RefreshAsync("clinic-test", CancellationToken.None);
            }

            object? count = await ScalarAsync(connection, "SELECT count(*) FROM \"Clinic\".\"mv_PatientSurgeries\";");
            Convert.ToInt64(count).Should().Be(2);
        }
        finally
        {
            await ExecuteAsync(connection, "DROP MATERIALIZED VIEW IF EXISTS \"Clinic\".\"mv_PatientSurgeries\"; DROP TABLE IF EXISTS \"Clinic\".\"SurgeryRefreshSource\";");
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

    private sealed class TestRequestContext : IRequestContext
    {
        public string Authority => "clinic.tests";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => null;
        public string? TenantId => "clinic-test";
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => "clinic-refresh-test";
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => null;
        public Request GetRequest() => new() { RequestId = "clinic-refresh-test", Authority = Authority, TenantId = TenantId };
    }
}
