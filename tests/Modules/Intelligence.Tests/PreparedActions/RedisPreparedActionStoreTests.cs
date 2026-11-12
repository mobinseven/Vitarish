using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using StackExchange.Redis;
using Testcontainers.Redis;
using Vitarish.Intelligence.Application.PreparedActions;
using Vitarish.Intelligence.Domain.PreparedActions;
using Vitarish.Intelligence.Infrastructure.Services.PreparedActions;
using Vitarish.Testing;

namespace Intelligence.Tests.PreparedActions;

public sealed class RedisPreparedActionStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    [DockerFact]
    public async Task Store_keeps_actions_under_its_own_namespace_with_retention_after_expiry()
    {
        await using RedisContainer redis = new RedisBuilder("redis:7.4-alpine").Build();
        await redis.StartAsync();
        await using ConnectionMultiplexer multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
        RedisPreparedActionStore store = new(multiplexer, new FakeTimeProvider(Now));
        PreparedAction action = Action();

        await store.SaveNewAsync(action, CancellationToken.None);

        IDatabase database = multiplexer.GetDatabase();
        RedisKey key = RedisPreparedActionStore.KeyPrefix + action.ActionId.ToString("D");
        (await database.KeyExistsAsync(key)).Should().BeTrue();
        TimeSpan? ttl = await database.KeyTimeToLiveAsync(key);
        ttl.Should().NotBeNull();
        ttl!.Value.Should().BeCloseTo(TimeSpan.FromMinutes(30) + TimeSpan.FromDays(1), TimeSpan.FromMinutes(1));
        StoredPreparedAction? stored = await store.GetAsync(action.ActionId, CancellationToken.None);
        stored!.Action.Should().Be(action);
        stored.Version.Should().Be((await database.StringGetAsync(key)).ToString());
    }

    [DockerFact]
    public async Task Saving_again_is_a_no_op_for_identical_data_and_refused_for_different_data()
    {
        await using RedisContainer redis = new RedisBuilder("redis:7.4-alpine").Build();
        await redis.StartAsync();
        await using ConnectionMultiplexer multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
        RedisPreparedActionStore store = new(multiplexer, new FakeTimeProvider(Now));
        PreparedAction action = Action();
        await store.SaveNewAsync(action, CancellationToken.None);

        await store.SaveNewAsync(action, CancellationToken.None);
        Func<Task> different = () => store.SaveNewAsync(action with { Summary = "changed" }, CancellationToken.None);

        await different.Should().ThrowAsync<InvalidOperationException>();
        (await store.GetAsync(action.ActionId, CancellationToken.None))!.Action.Summary.Should().Be(action.Summary);
    }

    [DockerFact]
    public async Task Replacement_succeeds_only_from_the_version_that_was_read_and_keeps_the_retention()
    {
        await using RedisContainer redis = new RedisBuilder("redis:7.4-alpine").Build();
        await redis.StartAsync();
        await using ConnectionMultiplexer multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
        RedisPreparedActionStore store = new(multiplexer, new FakeTimeProvider(Now));
        PreparedAction action = Action();
        await store.SaveNewAsync(action, CancellationToken.None);
        StoredPreparedAction read = (await store.GetAsync(action.ActionId, CancellationToken.None))!;

        bool claimed = await store.TryReplaceAsync(read, read.Action.BeginConfirmation(retryFailed: false)!, CancellationToken.None);
        bool staleReplace = await store.TryReplaceAsync(read, read.Action.Reject(Now, "late")!, CancellationToken.None);

        claimed.Should().BeTrue();
        staleReplace.Should().BeFalse();
        StoredPreparedAction latest = (await store.GetAsync(action.ActionId, CancellationToken.None))!;
        latest.Action.Status.Should().Be(PreparedActionStatus.Confirming);
        RedisKey key = RedisPreparedActionStore.KeyPrefix + action.ActionId.ToString("D");
        (await multiplexer.GetDatabase().KeyTimeToLiveAsync(key)).Should().NotBeNull();
    }

    [DockerFact]
    public async Task Replacing_a_missing_action_fails_and_does_not_create_it()
    {
        await using RedisContainer redis = new RedisBuilder("redis:7.4-alpine").Build();
        await redis.StartAsync();
        await using ConnectionMultiplexer multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
        RedisPreparedActionStore store = new(multiplexer, new FakeTimeProvider(Now));
        PreparedAction action = Action();

        bool replaced = await store.TryReplaceAsync(new(action, "{}"), action with { Status = PreparedActionStatus.Confirming }, CancellationToken.None);

        replaced.Should().BeFalse();
        (await store.GetAsync(action.ActionId, CancellationToken.None)).Should().BeNull();
    }

    [DockerFact]
    public async Task Concurrent_lifecycle_confirmations_claim_the_action_exactly_once()
    {
        await using RedisContainer redis = new RedisBuilder("redis:7.4-alpine").Build();
        await redis.StartAsync();
        await using ConnectionMultiplexer multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
        FakeTimeProvider time = new(Now);
        RedisPreparedActionStore store = new(multiplexer, time);
        PreparedAction action = Action();
        await store.SaveNewAsync(action, CancellationToken.None);
        PreparedActionLifecycle lifecycle = new(store, time);
        CountingApp app = new();

        PreparedActionResult[] results = await Task.WhenAll(Enumerable.Range(0, 16)
            .Select(_ => lifecycle.ConfirmAsync(action.ActionId, app, CancellationToken.None)));

        app.Commits.Should().Be(1);
        results.Should().OnlyContain(result =>
            result.Outcome == PreparedActionOutcome.Succeeded || result.Outcome == PreparedActionOutcome.Conflict);
        (await store.GetAsync(action.ActionId, CancellationToken.None))!.Action.Status.Should().Be(PreparedActionStatus.Completed);
    }

    private static PreparedAction Action() => new(
        Guid.NewGuid(), "agent-a", "scope-1", "patient", "subject-1", "conversation-1", "run-1", "user-1",
        "capability", "action-type", 1, "{\"review\":true}", "summary", "call-1", "tool", PreparedActionStatus.Proposed,
        Now, Now.AddMinutes(30));

    private sealed class CountingApp : IPreparedActionApp
    {
        private int _commits;
        public int Commits => _commits;
        public PreparedActionCaller Caller { get; } = new("agent-a", "scope-1", "patient", "subject-1", "user-1");
        public PreparedActionPolicy? ResolvePolicy(string actionType) => new(1, PreparedActionRetryClassification.Retryable);
        public bool Authorizes(string actionType) => true;
        public bool CanCommit(string actionType) => true;

        public async Task<PreparedActionCommitResult> CommitAsync(PreparedAction action, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _commits);
            await Task.Delay(20, cancellationToken);
            return PreparedActionCommitResult.Completed("{}");
        }
    }
}
