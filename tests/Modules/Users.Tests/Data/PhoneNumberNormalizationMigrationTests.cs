using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Tests.Security;

namespace Vitarish.Users.Tests.Data;

public sealed class PhoneNumberNormalizationMigrationTests
{
    private const string NormalizationMigrationId = "20261003043426_UsersPhoneNumberE164Normalization";

    [DockerFact]
    public async Task Sql_function_matches_the_shared_case_table()
    {
        await using PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await container.StartAsync();

        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();
        await ExecuteAsync(connection, PhoneNumberNormalizationSql.FunctionDefinition);

        foreach ((string input, string? expected) in PhoneNormalizationCases.All)
        {
            object? result = await ScalarAsync(
                connection, "SELECT pg_temp.vitarish_normalize_phone($1)", input);
            string? actual = result is null or DBNull ? null : (string)result;
            actual.Should().Be(expected, "the SQL function must agree with case '{0}'", input);
        }
    }

    [DockerFact]
    public async Task Migration_converts_local_formats_clears_unconvertible_numbers_and_rotates_only_changed_stamps()
    {
        await using PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await container.StartAsync();
        string connectionString = await CreateFreshDatabaseAsync(container);
        await using UsersDbContext context = CreateContext(connectionString);
        IMigrator migrator = context.GetService<IMigrator>();
        string[] migrations = context.Database.GetMigrations().ToArray();
        int index = NormalizationIndex(migrations);
        await migrator.MigrateAsync(migrations[index - 1]);

        context.Users.AddRange(
            SeedUser("local", phoneNumber: "09123456789", confirmed: true, securityStamp: "stamp-local", concurrencyStamp: "cc-local"),
            SeedUser("bare", phoneNumber: "9123456700", confirmed: true, securityStamp: "stamp-bare", concurrencyStamp: "cc-bare"),
            SeedUser("prefix00", phoneNumber: "0098 912 345 0000", confirmed: true, securityStamp: "stamp-prefix", concurrencyStamp: "cc-prefix"),
            SeedUser("landline", phoneNumber: "02112345678", confirmed: true, securityStamp: "stamp-landline", concurrencyStamp: "cc-landline"),
            SeedUser("junk", phoneNumber: "0912345678ration", confirmed: true, securityStamp: "stamp-junk", concurrencyStamp: "cc-junk"),
            SeedUser("foreign", phoneNumber: "+14155552671", confirmed: false, securityStamp: "stamp-foreign", concurrencyStamp: "cc-foreign"),
            SeedUser("nophone", phoneNumber: null, confirmed: false, securityStamp: "stamp-nophone", concurrencyStamp: "cc-nophone"));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await migrator.MigrateAsync(migrations[index]);
        await using UsersDbContext verification = CreateContext(connectionString);

        User local = await verification.Users.SingleAsync(user => user.Id == "local");
        local.PhoneNumber.Should().Be("+989123456789");
        local.PhoneNumberConfirmed.Should().BeTrue();
        local.SecurityStamp.Should().Be("stamp-local");
        local.ConcurrencyStamp.Should().NotBe("cc-local");

        User bare = await verification.Users.SingleAsync(user => user.Id == "bare");
        bare.PhoneNumber.Should().Be("+989123456700");
        bare.ConcurrencyStamp.Should().NotBe("cc-bare");

        User prefix00 = await verification.Users.SingleAsync(user => user.Id == "prefix00");
        prefix00.PhoneNumber.Should().Be("+989123450000");
        prefix00.ConcurrencyStamp.Should().NotBe("cc-prefix");

        User landline = await verification.Users.SingleAsync(user => user.Id == "landline");
        landline.PhoneNumber.Should().BeNull();
        landline.PhoneNumberConfirmed.Should().BeFalse();
        landline.SecurityStamp.Should().Be("stamp-landline");
        landline.ConcurrencyStamp.Should().NotBe("cc-landline");

        User junk = await verification.Users.SingleAsync(user => user.Id == "junk");
        junk.PhoneNumber.Should().BeNull();
        junk.PhoneNumberConfirmed.Should().BeFalse();
        junk.ConcurrencyStamp.Should().NotBe("cc-junk");

        User foreign = await verification.Users.SingleAsync(user => user.Id == "foreign");
        foreign.PhoneNumber.Should().Be("+14155552671");
        foreign.ConcurrencyStamp.Should().Be("cc-foreign");

        User nophone = await verification.Users.SingleAsync(user => user.Id == "nophone");
        nophone.PhoneNumber.Should().BeNull();
        nophone.ConcurrencyStamp.Should().Be("cc-nophone");

        string[] applied = (await verification.Database.GetAppliedMigrationsAsync()).ToArray();
        applied.Should().Contain(NormalizationMigrationId);
    }

    [DockerFact]
    public async Task Migration_stops_on_duplicate_numbers_and_leaves_the_data_unchanged()
    {
        await using PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await container.StartAsync();
        string connectionString = await CreateFreshDatabaseAsync(container);
        await using UsersDbContext context = CreateContext(connectionString);
        IMigrator migrator = context.GetService<IMigrator>();
        string[] migrations = context.Database.GetMigrations().ToArray();
        int index = NormalizationIndex(migrations);
        await migrator.MigrateAsync(migrations[index - 1]);

        context.Users.AddRange(
            SeedUser("dup-a", phoneNumber: "09123456789", confirmed: true, securityStamp: "stamp-a", concurrencyStamp: "cc-a"),
            SeedUser("dup-b", phoneNumber: "+989123456789", confirmed: true, securityStamp: "stamp-b", concurrencyStamp: "cc-b"));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Exception failure = await Record.ExceptionAsync(() => migrator.MigrateAsync(migrations[index])) ?? throw new InvalidOperationException("The migration was expected to fail.");

        failure.ToString().Should().Contain("collide after normalization");
        failure.ToString().Should().Contain("dup-a");
        failure.ToString().Should().Contain("dup-b");
        failure.ToString().Should().NotContain("09123456789");
        failure.ToString().Should().NotContain("+989123456789");

        await using UsersDbContext verification = CreateContext(connectionString);
        User first = await verification.Users.AsNoTracking().SingleAsync(user => user.Id == "dup-a");
        first.PhoneNumber.Should().Be("09123456789");
        first.PhoneNumberConfirmed.Should().BeTrue();
        first.ConcurrencyStamp.Should().Be("cc-a");
        User second = await verification.Users.AsNoTracking().SingleAsync(user => user.Id == "dup-b");
        second.PhoneNumber.Should().Be("+989123456789");
        second.ConcurrencyStamp.Should().Be("cc-b");

        string[] applied = (await verification.Database.GetAppliedMigrationsAsync()).ToArray();
        applied.Should().NotContain(NormalizationMigrationId);
    }

    private static int NormalizationIndex(string[] migrations)
    {
        int index = Array.FindIndex(migrations, migration => migration.EndsWith(NormalizationMigrationId, StringComparison.Ordinal));
        index.Should().BeGreaterThan(0, "the normalization migration must follow at least one earlier migration");
        return index;
    }

    private static User SeedUser(string id, string? phoneNumber, bool confirmed, string securityStamp, string concurrencyStamp) => new()
    {
        Id = id,
        UserName = id,
        NormalizedUserName = id.ToUpperInvariant(),
        PhoneNumber = phoneNumber,
        PhoneNumberConfirmed = confirmed,
        SecurityStamp = securityStamp,
        ConcurrencyStamp = concurrencyStamp
    };

    private static UsersDbContext CreateContext(string connectionString) =>
        PostgresTestSupport.CreateContextWithoutAccountStatus(connectionString);

    private static async Task<string> CreateFreshDatabaseAsync(PostgreSqlContainer container)
    {
        string hostConnectionString = container.GetConnectionString();
        string databaseName = "users_migration_" + Guid.NewGuid().ToString("N")[..12];

        await using NpgsqlConnection admin = new(new NpgsqlConnectionStringBuilder(hostConnectionString) { Database = "postgres" }.ConnectionString);
        await admin.OpenAsync();
        await ExecuteAsync(admin, $"CREATE DATABASE \"{databaseName}\"");

        return new NpgsqlConnectionStringBuilder(hostConnectionString) { Database = databaseName }.ConnectionString;
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using NpgsqlCommand command = new(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<object?> ScalarAsync(NpgsqlConnection connection, string sql, params object[] parameters)
    {
        await using NpgsqlCommand command = new(sql, connection);
        for (int i = 0; i < parameters.Length; i++)
            command.Parameters.AddWithValue(parameters[i]);
        return await command.ExecuteScalarAsync();
    }
}
