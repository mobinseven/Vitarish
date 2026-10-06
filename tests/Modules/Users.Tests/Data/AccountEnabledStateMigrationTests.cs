using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Tests.Data;

public sealed class AccountEnabledStateMigrationTests
{
    private const string MigrationId = "20261006075605_UsersAccountEnabledState";

    [DockerFact]
    public async Task Migration_keeps_pre_existing_accounts_enabled_and_allows_disable()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);

        await using UsersDbContext context = PostgresTestSupport.CreateContext(connectionString);
        IMigrator migrator = context.GetService<IMigrator>();
        string[] migrations = context.Database.GetMigrations().ToArray();
        int index = Array.FindIndex(migrations, migration => migration.EndsWith(MigrationId, StringComparison.Ordinal));
        index.Should().BeGreaterThan(0, "the account-state migration must follow the existing Users migrations");
        await migrator.MigrateAsync(migrations[index - 1]);

        await using (NpgsqlConnection connection = new(connectionString))
        {
            await connection.OpenAsync();
            await using NpgsqlCommand insert = new(
                """
                INSERT INTO "Users"."AspNetUsers" (
                    "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
                    "SecurityStamp", "ConcurrencyStamp", "AccessFailedCount",
                    "EmailConfirmed", "LockoutEnabled", "PhoneNumberConfirmed", "TwoFactorEnabled")
                VALUES (
                    'legacy-user', 'legacy@example.test', 'LEGACY@EXAMPLE.TEST', 'legacy@example.test', 'LEGACY@EXAMPLE.TEST',
                    'security-stamp', 'revision', 0,
                    FALSE, FALSE, FALSE, FALSE)
                """,
                connection);
            _ = await insert.ExecuteNonQueryAsync();
        }

        await migrator.MigrateAsync(migrations[index]);

        await using UsersDbContext verification = PostgresTestSupport.CreateContext(connectionString);
        User legacy = await verification.Users.AsNoTracking().SingleAsync(user => user.Id == "legacy-user");
        legacy.IsEnabled.Should().BeTrue();

        legacy.IsEnabled = false;
        context.Users.Attach(legacy);
        context.Entry(legacy).State = EntityState.Modified;
        await context.SaveChangesAsync();

        await using UsersDbContext disabledVerification = PostgresTestSupport.CreateContext(connectionString);
        (await disabledVerification.Users.AsNoTracking().SingleAsync(user => user.Id == "legacy-user")).IsEnabled.Should().BeFalse();
    }
}
