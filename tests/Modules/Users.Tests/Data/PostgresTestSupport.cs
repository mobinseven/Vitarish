using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Testcontainers.PostgreSql;
using Vitarish.Users.Domain.Entities;
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

    /// <summary>
    /// A context for databases migrated only up to a migration that predates the
    /// account status column, so seeding and verification match that schema.
    /// The context type stays <see cref="UsersDbContext"/> so its migrations resolve.
    /// </summary>
    internal static UsersDbContext CreateContextWithoutAccountStatus(string connectionString)
    {
        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql(connectionString)
            .ReplaceService<IModelCustomizer, ModelCustomizerWithoutAccountStatus>()
            .Options;
        return new UsersDbContext(options);
    }

    private sealed class ModelCustomizerWithoutAccountStatus(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
    {
        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);
            _ = modelBuilder.Entity<User>().Ignore(user => user.IsEnabled);
        }
    }

    internal static async Task ApplyAllMigrationsAsync(string connectionString)
    {
        await using UsersDbContext context = CreateContext(connectionString);
        await context.Database.MigrateAsync();
    }
}
