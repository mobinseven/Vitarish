using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vitarish.Users.Application.Contracts;
using Vitarish.Users.Application.TemporaryAutoAuthLinks;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Options;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Service;

namespace Users.Tests.Infrastructure;

public class TemporaryAutoAuthLinkServicesTests
{
    [Fact]
    public async Task IssueAsync_StoresOnlyCodeHash()
    {
        await using UsersDbContext context = CreateContext(nameof(IssueAsync_StoresOnlyCodeHash));
        _ = context.Users.Add(new User { Id = "user-1", UserName = "u1" });
        await context.SaveChangesAsync();

        TemporaryAutoAuthLinkIssuer issuer = new(context, CreateOptions());

        IssueTemporaryAutoAuthLinkResult result = await issuer.IssueAsync(
            new IssueTemporaryAutoAuthLinkRequest(
                "user-1",
                "/medical/patient-checkin",
                TemporaryAutoAuthLinkPurposes.AdvancedInputVoice));

        TemporaryAutoAuthLink stored = await context.TemporaryAutoAuthLinks.SingleAsync();
        string expectedHash = TemporaryAutoAuthLinkSecurity.ComputeCodeHash(result.RawCode, CreateOptions().Value);

        stored.CodeHash.Should().Be(expectedHash);
        stored.CodeHash.Should().NotBe(result.RawCode);
    }

    [Fact]
    public async Task IssueAsync_RejectsUnknownPurpose()
    {
        await using UsersDbContext context = CreateContext(nameof(IssueAsync_RejectsUnknownPurpose));
        _ = context.Users.Add(new User { Id = "user-1", UserName = "u1" });
        await context.SaveChangesAsync();
        TemporaryAutoAuthLinkIssuer issuer = new(context, CreateOptions());

        Func<Task> act = () => issuer.IssueAsync(
            new IssueTemporaryAutoAuthLinkRequest("user-1", "/medical/patient-checkin", "unknown"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task IssueAsync_RejectsNonLocalDestination()
    {
        await using UsersDbContext context = CreateContext(nameof(IssueAsync_RejectsNonLocalDestination));
        _ = context.Users.Add(new User { Id = "user-1", UserName = "u1" });
        await context.SaveChangesAsync();
        TemporaryAutoAuthLinkIssuer issuer = new(context, CreateOptions());

        Func<Task> act = () => issuer.IssueAsync(
            new IssueTemporaryAutoAuthLinkRequest(
                "user-1",
                "https://evil.example/a",
                TemporaryAutoAuthLinkPurposes.AdvancedInputVoice));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task IssueAsync_RejectsPurposeDisallowedDestination()
    {
        await using UsersDbContext context = CreateContext(nameof(IssueAsync_RejectsPurposeDisallowedDestination));
        _ = context.Users.Add(new User { Id = "user-1", UserName = "u1" });
        await context.SaveChangesAsync();
        TemporaryAutoAuthLinkIssuer issuer = new(context, CreateOptions(["/medical/allowed"]));

        Func<Task> act = () => issuer.IssueAsync(
            new IssueTemporaryAutoAuthLinkRequest(
                "user-1",
                "/medical/denied",
                TemporaryAutoAuthLinkPurposes.AdvancedInputVoice));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RedeemAsync_RejectsExpiredLink()
    {
        await using UsersDbContext context = CreateContext(nameof(RedeemAsync_RejectsExpiredLink));
        string rawCode = "raw-code";
        string codeHash = TemporaryAutoAuthLinkSecurity.ComputeCodeHash(rawCode, CreateOptions().Value);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        _ = context.TemporaryAutoAuthLinks.Add(TemporaryAutoAuthLink.Create(
            Guid.NewGuid(),
            codeHash,
            "user-1",
            "/medical/ok",
            TemporaryAutoAuthLinkPurposes.AdvancedInputVoice,
            now.AddMinutes(-20),
            now.AddMinutes(-10)));
        await context.SaveChangesAsync();

        TemporaryAutoAuthLinkRedeemer redeemer = new(context, CreateUserManager("user-1"), CreateOptions());

        RedeemTemporaryAutoAuthLinkResult result = await redeemer.RedeemAsync(
            new RedeemTemporaryAutoAuthLinkRequest(rawCode, TemporaryAutoAuthLinkPurposes.AdvancedInputVoice));

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task RedeemAsync_RejectsAlreadyConsumedLink()
    {
        await using UsersDbContext context = CreateContext(nameof(RedeemAsync_RejectsAlreadyConsumedLink));
        string rawCode = "raw-code";
        string codeHash = TemporaryAutoAuthLinkSecurity.ComputeCodeHash(rawCode, CreateOptions().Value);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        TemporaryAutoAuthLink link = TemporaryAutoAuthLink.Create(
            Guid.NewGuid(),
            codeHash,
            "user-1",
            "/medical/ok",
            TemporaryAutoAuthLinkPurposes.AdvancedInputVoice,
            now.AddMinutes(-2),
            now.AddMinutes(10));
        link.MarkConsumed(now.AddMinutes(-1), "127.0.0.1", "ua");
        _ = context.TemporaryAutoAuthLinks.Add(link);
        await context.SaveChangesAsync();

        TemporaryAutoAuthLinkRedeemer redeemer = new(context, CreateUserManager("user-1"), CreateOptions());

        RedeemTemporaryAutoAuthLinkResult result = await redeemer.RedeemAsync(
            new RedeemTemporaryAutoAuthLinkRequest(rawCode, TemporaryAutoAuthLinkPurposes.AdvancedInputVoice));

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task RedeemAsync_ConsumesValidLinkAndStoresClientData()
    {
        await using UsersDbContext context = CreateContext(nameof(RedeemAsync_ConsumesValidLinkAndStoresClientData));
        string rawCode = "raw-code";
        string codeHash = TemporaryAutoAuthLinkSecurity.ComputeCodeHash(rawCode, CreateOptions().Value);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        _ = context.TemporaryAutoAuthLinks.Add(TemporaryAutoAuthLink.Create(
            Guid.NewGuid(),
            codeHash,
            "user-1",
            "/medical/ok",
            TemporaryAutoAuthLinkPurposes.AdvancedInputVoice,
            now.AddMinutes(-1),
            now.AddMinutes(10)));
        await context.SaveChangesAsync();

        TemporaryAutoAuthLinkRedeemer redeemer = new(context, CreateUserManager("user-1"), CreateOptions());

        RedeemTemporaryAutoAuthLinkResult result = await redeemer.RedeemAsync(
            new RedeemTemporaryAutoAuthLinkRequest(
                rawCode,
                TemporaryAutoAuthLinkPurposes.AdvancedInputVoice,
                "127.0.0.1",
                "agent"));

        result.Succeeded.Should().BeTrue();
        TemporaryAutoAuthLink stored = await context.TemporaryAutoAuthLinks.SingleAsync();
        stored.ConsumedAt.Should().NotBeNull();
        stored.ConsumedByIp.Should().Be("127.0.0.1");
        stored.ConsumedByUserAgent.Should().Be("agent");
    }

    [Fact]
    public async Task RedeemAsync_TwoSimultaneousAttempts_OnlyOneSucceeds()
    {
        string dbName = nameof(RedeemAsync_TwoSimultaneousAttempts_OnlyOneSucceeds);
        await using (UsersDbContext setupContext = CreateContext(dbName))
        {
            string rawCode = "raw-code";
            string codeHash = TemporaryAutoAuthLinkSecurity.ComputeCodeHash(rawCode, CreateOptions().Value);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            _ = setupContext.TemporaryAutoAuthLinks.Add(TemporaryAutoAuthLink.Create(
                Guid.NewGuid(),
                codeHash,
                "user-1",
                "/medical/ok",
                TemporaryAutoAuthLinkPurposes.AdvancedInputVoice,
                now.AddMinutes(-1),
                now.AddMinutes(10)));
            await setupContext.SaveChangesAsync();
        }

        await using UsersDbContext context1 = CreateContext(dbName);
        await using UsersDbContext context2 = CreateContext(dbName);
        TemporaryAutoAuthLinkRedeemer redeemer1 = new(context1, CreateUserManager("user-1"), CreateOptions());
        TemporaryAutoAuthLinkRedeemer redeemer2 = new(context2, CreateUserManager("user-1"), CreateOptions());
        RedeemTemporaryAutoAuthLinkRequest request = new("raw-code", TemporaryAutoAuthLinkPurposes.AdvancedInputVoice);

        Task<RedeemTemporaryAutoAuthLinkResult> first = redeemer1.RedeemAsync(request);
        Task<RedeemTemporaryAutoAuthLinkResult> second = redeemer2.RedeemAsync(request);
        RedeemTemporaryAutoAuthLinkResult[] results = await Task.WhenAll(first, second);

        results.Count(r => r.Succeeded).Should().Be(1);
    }

    [Fact]
    public async Task RedeemAsync_InvalidRawCode_ReturnsGenericFailure()
    {
        await using UsersDbContext context = CreateContext(nameof(RedeemAsync_InvalidRawCode_ReturnsGenericFailure));
        TemporaryAutoAuthLinkRedeemer redeemer = new(context, CreateUserManager("user-1"), CreateOptions());

        RedeemTemporaryAutoAuthLinkResult result = await redeemer.RedeemAsync(
            new RedeemTemporaryAutoAuthLinkRequest("invalid", TemporaryAutoAuthLinkPurposes.AdvancedInputVoice));

        result.Succeeded.Should().BeFalse();
        result.UserId.Should().BeNull();
    }

    [Fact]
    public async Task CleanupAsync_RemovesEligibleRecords()
    {
        await using UsersDbContext context = CreateContext(nameof(CleanupAsync_RemovesEligibleRecords));
        DateTimeOffset now = DateTimeOffset.UtcNow;

        TemporaryAutoAuthLink consumedOld = TemporaryAutoAuthLink.Create(
            Guid.NewGuid(),
            "h1",
            "user-1",
            "/ok",
            TemporaryAutoAuthLinkPurposes.AdvancedInputVoice,
            now.AddDays(-10),
            now.AddDays(-9));
        consumedOld.MarkConsumed(now.AddDays(-8), null, null);

        TemporaryAutoAuthLink expiredOld = TemporaryAutoAuthLink.Create(
            Guid.NewGuid(),
            "h2",
            "user-1",
            "/ok",
            TemporaryAutoAuthLinkPurposes.AdvancedInputVoice,
            now.AddDays(-10),
            now.AddDays(-8));

        TemporaryAutoAuthLink active = TemporaryAutoAuthLink.Create(
            Guid.NewGuid(),
            "h3",
            "user-1",
            "/ok",
            TemporaryAutoAuthLinkPurposes.AdvancedInputVoice,
            now.AddMinutes(-1),
            now.AddMinutes(30));

        _ = context.TemporaryAutoAuthLinks.AddRange(consumedOld, expiredOld, active);
        await context.SaveChangesAsync();

        TemporaryAutoAuthLinkCleanup cleanup = new(context, CreateOptions());

        int deleted = await cleanup.CleanupAsync(now);

        deleted.Should().Be(2);
        (await context.TemporaryAutoAuthLinks.CountAsync()).Should().Be(1);
    }

    private static UsersDbContext CreateContext(string dbName)
    {
        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new UsersDbContext(options);
    }

    private static IOptions<TemporaryAutoAuthLinkOptions> CreateOptions(params string[] allowedPrefixes) =>
        Options.Create(new TemporaryAutoAuthLinkOptions
        {
            HmacSecret = "unit-test-secret",
            DefaultLifetimeMinutes = 10,
            RetentionDays = 7,
            Purposes = new Dictionary<string, TemporaryAutoAuthLinkPurposeOptions>(StringComparer.Ordinal)
            {
                [TemporaryAutoAuthLinkPurposes.AdvancedInputVoice] = new TemporaryAutoAuthLinkPurposeOptions
                {
                    LifetimeMinutes = 10,
                    AllowedDestinationPathPrefixes = allowedPrefixes.Length == 0 ? ["/medical"] : allowedPrefixes
                }
            }
        });

    private static UserManager<User> CreateUserManager(string userId)
    {
        Mock<UserManager<User>> mock = UserManagerMockFactory.Create();
        mock.Setup(m => m.FindByIdAsync(userId))
            .ReturnsAsync(new User { Id = userId, UserName = "user" });
        mock.Setup(m => m.IsLockedOutAsync(It.IsAny<User>()))
            .ReturnsAsync(false);
        return mock.Object;
    }
}
