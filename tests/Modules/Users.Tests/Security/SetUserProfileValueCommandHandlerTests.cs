using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Handlers;

namespace Vitarish.Users.Tests.Security;

public sealed class SetUserProfileValueCommandHandlerTests
{
    private const string DraftKey = "parties.onboarding.setup-draft";
    private readonly string databaseName = Guid.NewGuid().ToString("N");

    [Fact]
    public async Task Sets_one_entry_and_keeps_every_other_entry()
    {
        await SeedAsync(new() { ["Culture"] = "fa", ["other.entry"] = "kept" });

        SetUserProfileValueOutcome outcome = await HandleAsync(new("user-1", DraftKey, "{}"));

        outcome.Should().Be(SetUserProfileValueOutcome.Saved);
        (await StoredAsync()).Should().Equal(new Dictionary<string, string>
        {
            ["Culture"] = "fa",
            ["other.entry"] = "kept",
            [DraftKey] = "{}",
        });
    }

    [Fact]
    public async Task A_null_value_removes_only_that_entry()
    {
        await SeedAsync(new() { ["Culture"] = "en", [DraftKey] = "{}" });

        _ = await HandleAsync(new("user-1", DraftKey, null));

        (await StoredAsync()).Should().Equal(new Dictionary<string, string> { ["Culture"] = "en" });
    }

    [Theory]
    [InlineData("Culture")]
    [InlineData("no-namespace")]
    [InlineData("Parties.Draft")]
    public async Task Refuses_keys_that_could_overwrite_the_profiles_own_settings(string key)
    {
        await SeedAsync(new() { ["Culture"] = "en" });

        SetUserProfileValueOutcome outcome = await HandleAsync(new("user-1", key, "x"));

        outcome.Should().Be(SetUserProfileValueOutcome.Invalid);
        (await StoredAsync()).Should().Equal(new Dictionary<string, string> { ["Culture"] = "en" });
    }

    [Fact]
    public async Task Writes_only_while_the_entry_still_holds_the_expected_value()
    {
        await SeedAsync(new() { [DraftKey] = "changed meanwhile" });

        SetUserProfileValueOutcome stale = await HandleAsync(new("user-1", DraftKey, "mine", new ProfileValueExpectation("as I read it")));
        SetUserProfileValueOutcome absent = await HandleAsync(new("user-1", DraftKey, "mine", new ProfileValueExpectation(null)));
        SetUserProfileValueOutcome current = await HandleAsync(new("user-1", DraftKey, "mine", new ProfileValueExpectation("changed meanwhile")));

        stale.Should().Be(SetUserProfileValueOutcome.Conflict);
        absent.Should().Be(SetUserProfileValueOutcome.Conflict);
        current.Should().Be(SetUserProfileValueOutcome.Saved);
        (await StoredAsync())[DraftKey].Should().Be("mine");
    }

    [Fact]
    public async Task Reports_a_conflict_when_another_change_landed_first()
    {
        await SeedAsync([]);
        await using TestUsersDbContext slow = Context();
        _ = await slow.Users.SingleAsync(user => user.Id == "user-1");
        SetUserProfileValueCommandHandler handler = new(slow, Cache());

        // Another writer saves between this handler's read and its write.
        await using (TestUsersDbContext other = Context())
        {
            User user = await other.Users.SingleAsync(candidate => candidate.Id == "user-1");
            user.ProfilePreferences = new() { ["other.entry"] = "theirs" };
            user.ConcurrencyStamp = Guid.NewGuid().ToString();
            _ = await other.SaveChangesAsync();
        }

        SetUserProfileValueOutcome outcome = await handler.Handle(new("user-1", DraftKey, "{}"), default);

        outcome.Should().Be(SetUserProfileValueOutcome.Conflict);
        (await StoredAsync()).Should().Equal(new Dictionary<string, string> { ["other.entry"] = "theirs" });
    }

    [Fact]
    public async Task Reports_an_unknown_user()
    {
        await SeedAsync([]);

        (await HandleAsync(new("missing", DraftKey, "{}"))).Should().Be(SetUserProfileValueOutcome.UserNotFound);
    }

    private async Task<SetUserProfileValueOutcome> HandleAsync(SetUserProfileValueCommand command)
    {
        await using TestUsersDbContext dbContext = Context();
        return await new SetUserProfileValueCommandHandler(dbContext, Cache()).Handle(command, default);
    }

    private async Task SeedAsync(Dictionary<string, string> profile)
    {
        await using TestUsersDbContext dbContext = Context();
        _ = dbContext.Users.Add(new User { Id = "user-1", UserName = "user-1", ConcurrencyStamp = Guid.NewGuid().ToString(), ProfilePreferences = profile });
        _ = await dbContext.SaveChangesAsync();
    }

    private async Task<Dictionary<string, string>> StoredAsync()
    {
        await using TestUsersDbContext dbContext = Context();
        return (await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == "user-1")).ProfilePreferences;
    }

    private TestUsersDbContext Context() =>
        new(new DbContextOptionsBuilder<UsersDbContext>().UseInMemoryDatabase(databaseName).Options);

    private static IDistributedCache Cache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    private sealed class TestUsersDbContext(DbContextOptions<UsersDbContext> options) : UsersDbContext(options);
}
