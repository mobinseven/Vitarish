using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Tests.Data;

public sealed class UserNameFollowsContactIdentifierMigrationTests
{
    private const string MigrationId = "20261003115416_UsersUserNameFollowsContactIdentifier";

    [DockerFact]
    public async Task Migration_moves_stale_user_names_skips_collisions_and_is_idempotent()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);

        await using UsersDbContext context = PostgresTestSupport.CreateContext(connectionString);
        IMigrator migrator = context.GetService<IMigrator>();
        string[] migrations = context.Database.GetMigrations().ToArray();
        int index = Array.FindIndex(migrations, migration => migration.EndsWith(MigrationId, StringComparison.Ordinal));
        index.Should().BeGreaterThan(0, "the user-name migration must follow the unique-identifier migration");
        await migrator.MigrateAsync(migrations[index - 1]);

        context.Users.AddRange(
            Seed("stale-email", "old@example.test", "new@example.test", "NEW@EXAMPLE.TEST", null),
            Seed("stale-phone", "09123456789", null, null, "+989123456700"),
            Seed("email-cleared", "gone@example.test", null, null, "+9891211111111"),
            Seed("phone-cleared", "+9891244444444", "current@example.test", "CURRENT@EXAMPLE.TEST", null),
            Seed("taken-stale", "stale-taken@example.test", "taken@example.test", null, null),
            Seed("taken-owner", "taken@example.test", null, null, null),
            Seed("already-correct", "steady@example.test", "steady@example.test", "STEADY@EXAMPLE.TEST", null),
            Seed("unrelated", "independently-chosen", "owner@example.test", "OWNER@EXAMPLE.TEST", null));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await migrator.MigrateAsync(migrations[index]);

        await using UsersDbContext verification = PostgresTestSupport.CreateContext(connectionString);
        (await verification.Users.SingleAsync(user => user.Id == "stale-email")).Should().Match<User>(user =>
            user.UserName == "new@example.test" && user.NormalizedUserName == "NEW@EXAMPLE.TEST");
        (await verification.Users.SingleAsync(user => user.Id == "stale-phone")).UserName.Should().Be("+989123456700");
        (await verification.Users.SingleAsync(user => user.Id == "email-cleared")).UserName.Should().Be("+9891211111111");
        (await verification.Users.SingleAsync(user => user.Id == "phone-cleared")).UserName.Should().Be("current@example.test");
        (await verification.Users.SingleAsync(user => user.Id == "taken-stale")).UserName.Should().Be("stale-taken@example.test");
        (await verification.Users.SingleAsync(user => user.Id == "taken-owner")).UserName.Should().Be("taken@example.test");
        (await verification.Users.SingleAsync(user => user.Id == "already-correct")).UserName.Should().Be("steady@example.test");
        (await verification.Users.SingleAsync(user => user.Id == "unrelated")).UserName.Should().Be("independently-chosen");

        Dictionary<string, (string UserName, string? NormalizedUserName)> afterFirstRun = await SnapshotAsync(connectionString);
        List<string> notices = [];
        await using (NpgsqlConnection connection = new(connectionString))
        {
            connection.Notice += (_, args) => notices.Add(args.Notice.MessageText);
            await connection.OpenAsync();
            await using NpgsqlCommand command = new(UserNameFollowsContactIdentifierSql.Script, connection);
            await command.ExecuteNonQueryAsync();
        }

        Dictionary<string, (string UserName, string? NormalizedUserName)> afterSecondRun = await SnapshotAsync(connectionString);
        afterSecondRun.Should().Equal(afterFirstRun);

        string collisionNotice = notices.Single(notice => notice.Contains("taken-stale", StringComparison.Ordinal));
        collisionNotice.Should().Contain("account ID");
        collisionNotice.Should().NotContain("stale-taken@example.test");
        collisionNotice.Should().NotContain("taken@example.test");
    }

    private static async Task<Dictionary<string, (string UserName, string? NormalizedUserName)>> SnapshotAsync(string connectionString)
    {
        await using UsersDbContext context = PostgresTestSupport.CreateContext(connectionString);
        List<User> users = await context.Users.AsNoTracking()
            .OrderBy(user => user.Id)
            .ToListAsync();
        return users.ToDictionary(
            user => user.Id,
            user => (user.UserName!, user.NormalizedUserName));
    }

    private static User Seed(string id, string userName, string? email, string? normalizedEmail, string? phoneNumber) => new()
    {
        Id = id,
        UserName = userName,
        NormalizedUserName = userName.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = normalizedEmail,
        PhoneNumber = phoneNumber,
        SecurityStamp = "security-" + id,
        ConcurrencyStamp = "rev-" + id
    };
}
