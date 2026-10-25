using FluentAssertions;
using StackExchange.Redis;
using Testcontainers.Redis;
using Vitarish.Audit.Application;
using Vitarish.Main.Infrastructure.BackgroundJobs;

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
        await counter.RecordFailedWriteAsync(started, CancellationToken.None);
        await counter.RecordFailedWriteAsync(started.AddMinutes(5), CancellationToken.None);
        await counter.RecordFailedWriteAsync(started.AddMinutes(3), CancellationToken.None);

        AuditStorageOutage outage = (await counter.GetAsync(CancellationToken.None))!;
        outage.Should().Be(new AuditStorageOutage(started, started.AddMinutes(5), 3));

        await counter.RecordFailedWriteAsync(started.AddMinutes(6), CancellationToken.None);
        (await counter.ClearAsync(outage, CancellationToken.None)).Should().BeFalse("a write failed after the outage was read");
        AuditStorageOutage latest = (await counter.GetAsync(CancellationToken.None))!;
        (await counter.ClearAsync(latest, CancellationToken.None)).Should().BeTrue();
        (await counter.GetAsync(CancellationToken.None)).Should().BeNull();
    }

    private sealed class DockerFactAttribute : FactAttribute
    {
        public DockerFactAttribute()
        {
            if (!IsDockerAvailable())
                Skip = "Redis integration coverage requires an available Docker engine.";
        }

        private static bool IsDockerAvailable() =>
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST")) ||
            (OperatingSystem.IsWindows()
                ? File.Exists(@"\.\pipe\docker_engine") || File.Exists(@"\.\pipe\dockerDesktopLinuxEngine")
                : File.Exists("/var/run/docker.sock"));
    }
}
