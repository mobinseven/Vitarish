using FluentAssertions;
using StackExchange.Redis;
using Testcontainers.Redis;
using Vitarish.Audit.Application;
using Vitarish.Main.Infrastructure.BackgroundJobs;
using Vitarish.Testing;

namespace Main.Tests;

public sealed class RedisAuditStorageOutageCounterTests
{
    [DockerFact]
    public async Task Redis_keeps_the_first_failure_the_latest_failure_and_the_count_until_a_matching_clear()
    {
        await using RedisContainer redis = new RedisBuilder("redis:7.4-alpine").Build();
        await redis.StartAsync();
        await using ConnectionMultiplexer multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
        RedisAuditStorageOutageCounter counter = new(multiplexer);
        DateTime started = new(2026, 10, 25, 9, 0, 0, DateTimeKind.Utc);

        (await counter.GetAsync(CancellationToken.None)).Should().BeNull();
        await counter.RecordFailedWriteAsync(started.AddMinutes(5), CancellationToken.None);
        await counter.RecordFailedWriteAsync(started.AddMinutes(3), CancellationToken.None);
        await counter.RecordFailedWriteAsync(started, CancellationToken.None);

        AuditStorageOutage outage = (await counter.ReserveSummaryAsync(CancellationToken.None))!;
        outage.Should().Be(new AuditStorageOutage(started, started.AddMinutes(5), 3));

        await counter.RecordFailedWriteAsync(started.AddMinutes(6), CancellationToken.None);
        (await counter.ReserveSummaryAsync(CancellationToken.None)).Should().Be(outage, "a pending snapshot is stable across retries");
        (await counter.ClearAsync(outage, CancellationToken.None)).Should().BeTrue();
        RedisAuditStorageOutageCounter restarted = new(multiplexer);
        AuditStorageOutage latest = (await restarted.ReserveSummaryAsync(CancellationToken.None))!;
        latest.Should().Be(new AuditStorageOutage(started.AddMinutes(6), started.AddMinutes(6), 1));
        (await counter.ClearAsync(outage, CancellationToken.None)).Should().BeFalse("an old acknowledgement cannot clear a later snapshot");
        (await restarted.ClearAsync(latest, CancellationToken.None)).Should().BeTrue();
        (await counter.GetAsync(CancellationToken.None)).Should().BeNull();
    }
}
