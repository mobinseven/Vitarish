using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Tests.Data;

internal sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (!IsDockerAvailable())
            Skip = "PostgreSQL integration coverage requires an available Docker engine.";
    }

    private static bool IsDockerAvailable() =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST")) ||
        (OperatingSystem.IsWindows()
            ? File.Exists(@"\\.\pipe\docker_engine") || File.Exists(@"\\.\pipe\dockerDesktopLinuxEngine")
            : File.Exists("/var/run/docker.sock"));
}

internal static class PostgresTestSupport
{
    internal const string PostgresImage = "postgres:17-alpine";

    internal static PostgreSqlContainer CreateContainer() => new PostgreSqlBuilder(PostgresImage).Build();

    internal static async Task<string> CreateDatabaseAsync(PostgreSqlContainer container)
    {
        string hostConnectionString = container.GetConnectionString();
        string databaseName = "vitarish_test_" + Guid.NewGuid().ToString("N")[..12];

        await using Npgsql.NpgsqlConnection admin = new(
            new Npgsql.NpgsqlConnectionStringBuilder(hostConnectionString) { Database = "postgres" }.ConnectionString);
        await admin.OpenAsync();
        await using Npgsql.NpgsqlCommand command = new($"CREATE DATABASE \"{databaseName}\"", admin);
        _ = await command.ExecuteNonQueryAsync();

        return new Npgsql.NpgsqlConnectionStringBuilder(hostConnectionString) { Database = databaseName }.ConnectionString;
    }

    internal static UsersDbContext CreateContext(string connectionString)
    {
        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new UsersDbContext(options);
    }

    internal static async Task ApplyAllMigrationsAsync(string connectionString)
    {
        await using UsersDbContext context = CreateContext(connectionString);
        await context.Database.MigrateAsync();
    }
}
