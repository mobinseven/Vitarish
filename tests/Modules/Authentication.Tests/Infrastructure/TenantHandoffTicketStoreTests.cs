using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Authentication.Infrastructure;

namespace Authentication.Tests.Infrastructure;

[Collection(AuthenticationPostgresCollection.Name)]
public sealed class TenantHandoffTicketStoreTests
{
    private static readonly DateTime FixedNow = new(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AddAsync_stores_the_ticket_and_prunes_only_the_users_expired_or_redeemed_tickets()
    {
        DbContextOptions<AuthenticationDbContext> options = CreateOptions();
        await using AuthenticationDbContext db = new(options);
        TenantHandoffTicketStore store = new(db);

        TenantHandoffTicket userExpired = TenantHandoffTicket.Issue("hash-user-1-expired", "user-1", "rezaei.clinic.example.test", FixedNow.AddMinutes(-2));
        TenantHandoffTicket userRedeemed = TenantHandoffTicket.Issue("hash-user-1-redeemed", "user-1", "rezaei.clinic.example.test", FixedNow.AddSeconds(-10));
        userRedeemed.Redeem(FixedNow.AddSeconds(-5));
        TenantHandoffTicket userStillValid = TenantHandoffTicket.Issue("hash-user-1-valid", "user-1", "rezaei.clinic.example.test", FixedNow.AddSeconds(-10));
        TenantHandoffTicket otherUserExpired = TenantHandoffTicket.Issue("hash-user-2-expired", "user-2", "other.clinic.example.test", FixedNow.AddMinutes(-2));
        TenantHandoffTicket otherUserRedeemed = TenantHandoffTicket.Issue("hash-user-2-redeemed", "user-2", "other.clinic.example.test", FixedNow.AddSeconds(-10));
        otherUserRedeemed.Redeem(FixedNow.AddSeconds(-5));

        db.TenantHandoffTickets.AddRange(userExpired, userRedeemed, userStillValid, otherUserExpired, otherUserRedeemed);
        await db.SaveChangesAsync();

        TenantHandoffTicket added = TenantHandoffTicket.Issue("hash-user-1-added", "user-1", "rezaei.clinic.example.test", FixedNow);
        await store.AddAsync(added, FixedNow, CancellationToken.None);

        Guid[] remainingIds = await db.TenantHandoffTickets.AsNoTracking().Select(ticket => ticket.Id).ToArrayAsync();
        remainingIds.Should().BeEquivalentTo([added.Id, userStillValid.Id, otherUserExpired.Id, otherUserRedeemed.Id]);
    }

    [Fact]
    public async Task FindByTokenHashAsync_finds_matching_ticket_by_hash()
    {
        DbContextOptions<AuthenticationDbContext> options = CreateOptions();
        await using AuthenticationDbContext db = new(options);
        TenantHandoffTicketStore store = new(db);

        TenantHandoffTicket ticket = TenantHandoffTicket.Issue("hash-target", "user-1", "rezaei.clinic.example.test", FixedNow);
        await store.AddAsync(ticket, FixedNow, CancellationToken.None);

        TenantHandoffTicket? found = await store.FindByTokenHashAsync("hash-target", CancellationToken.None);
        TenantHandoffTicket? missing = await store.FindByTokenHashAsync("hash-other", CancellationToken.None);

        found.Should().NotBeNull();
        found!.Id.Should().Be(ticket.Id);
        found.UserId.Should().Be("user-1");
        missing.Should().BeNull();
    }

    [Fact]
    public Task TryMarkRedeemedAsync_succeeds_for_first_context_and_returns_false_for_concurrent_second_context() =>
        AssertOnlyFirstConcurrentRedemptionSucceedsAsync(CreateOptions());

    [PostgresFact]
    public async Task Postgres_allows_only_the_first_of_two_concurrent_redemptions()
    {
        DbContextOptions<AuthenticationDbContext> options = new DbContextOptionsBuilder<AuthenticationDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable(PostgresFactAttribute.ConnectionVariable))
            .Options;
        await using (AuthenticationDbContext setup = new(options))
        {
            await setup.Database.MigrateAsync();
            _ = await setup.TenantHandoffTickets.ExecuteDeleteAsync();
        }

        await AssertOnlyFirstConcurrentRedemptionSucceedsAsync(options);
    }

    private static async Task AssertOnlyFirstConcurrentRedemptionSucceedsAsync(DbContextOptions<AuthenticationDbContext> options)
    {
        await using (AuthenticationDbContext seedDb = new(options))
        {
            TenantHandoffTicketStore seedStore = new(seedDb);
            await seedStore.AddAsync(
                TenantHandoffTicket.Issue("hash-concurrent", "user-1", "rezaei.clinic.example.test", FixedNow),
                FixedNow,
                CancellationToken.None);
        }

        await using AuthenticationDbContext firstDb = new(options);
        await using AuthenticationDbContext secondDb = new(options);
        TenantHandoffTicketStore firstStore = new(firstDb);
        TenantHandoffTicketStore secondStore = new(secondDb);

        TenantHandoffTicket? firstLoaded = await firstStore.FindByTokenHashAsync("hash-concurrent", CancellationToken.None);
        TenantHandoffTicket? secondLoaded = await secondStore.FindByTokenHashAsync("hash-concurrent", CancellationToken.None);

        firstLoaded.Should().NotBeNull();
        secondLoaded.Should().NotBeNull();

        bool firstRedeemed = await firstStore.TryMarkRedeemedAsync(firstLoaded!, FixedNow.AddSeconds(5), CancellationToken.None);
        bool secondRedeemed = await secondStore.TryMarkRedeemedAsync(secondLoaded!, FixedNow.AddSeconds(6), CancellationToken.None);

        firstRedeemed.Should().BeTrue();
        secondRedeemed.Should().BeFalse();

        await using AuthenticationDbContext verifyDb = new(options);
        TenantHandoffTicket persisted = await verifyDb.TenantHandoffTickets.SingleAsync();
        persisted.RedeemedAt.Should().Be(FixedNow.AddSeconds(5));
    }

    private static DbContextOptions<AuthenticationDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<AuthenticationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

    private sealed class PostgresFactAttribute : FactAttribute
    {
        public const string ConnectionVariable = "VITARISH_AUTH_TEST_CONNECTION";

        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionVariable)))
                Skip = $"PostgreSQL integration coverage requires {ConnectionVariable}.";
        }
    }
}
