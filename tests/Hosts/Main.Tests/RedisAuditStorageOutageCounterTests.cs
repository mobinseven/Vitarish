using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;
using Testcontainers.Redis;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Main.Infrastructure.BackgroundJobs;
using Vitarish.Modules.Base.Core.BackgroundJobs;
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

    [DockerFact]
    public async Task A_reconnected_redis_counter_recovers_the_outage_with_one_audit_summary()
    {
        await using RedisContainer redis = new RedisBuilder("redis:7.4-alpine").Build();
        await redis.StartAsync();
        DateTime started = new(2026, 10, 31, 9, 0, 0, DateTimeKind.Utc);
        DateTime lastFailed = started.AddMinutes(4);

        await using (ConnectionMultiplexer firstConnection = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString()))
        {
            RedisAuditStorageOutageCounter firstCounter = new(firstConnection);
            await firstCounter.RecordFailedWriteAsync(started, CancellationToken.None);
            await firstCounter.RecordFailedWriteAsync(started.AddMinutes(2), CancellationToken.None);
            await firstCounter.RecordFailedWriteAsync(lastFailed, CancellationToken.None);
        }

        await using ConnectionMultiplexer reconnectedRedis = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
        RedisAuditStorageOutageCounter reconnectedCounter = new(reconnectedRedis);
        (await reconnectedCounter.GetAsync(CancellationToken.None))
            .Should().Be(new AuditStorageOutage(started, lastFailed, 3), "the counter is backed by Redis and remains available to a new client");

        Dictionary<string, SecurityAuditEvent> recordedAuditEvents = new(StringComparer.Ordinal);
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(item => item.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityAuditEvent auditEvent, CancellationToken _) =>
                recordedAuditEvents.TryAdd(auditEvent.IdempotencyKey, auditEvent)
                    ? SecurityAuditWriteResult.Recorded
                    : SecurityAuditWriteResult.AlreadyRecorded);
        using ServiceProvider services = CreateReporterServices(writer);

        await CreateReporter(reconnectedCounter, services).CheckOnceAsync(CancellationToken.None);
        await CreateReporter(reconnectedCounter, services).CheckOnceAsync(CancellationToken.None);

        SecurityAuditEvent summary = recordedAuditEvents.Values.Should().ContainSingle().Subject;
        summary.EventName.Should().Be(SecurityAuditEventCatalog.SecurityFailureEvent);
        summary.Outcome.Should().Be(SecurityAuditOutcome.Failed);
        summary.ActorKind.Should().Be(SecurityAuditActorKind.System);
        summary.TenantId.Should().Be(SecurityAuditScopeCatalog.PlatformScopeId);
        summary.RedactedReason.Should().Be(SecurityAuditReasonCatalog.AuditStorageOutage(started, lastFailed, 3));
        writer.Verify(item => item.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        (await reconnectedCounter.GetAsync(CancellationToken.None)).Should().BeNull();
    }

    private static ServiceProvider CreateReporterServices(Mock<ISecurityAuditWriter> writer)
    {
        ServiceCollection services = new();
        _ = services.AddSingleton<IAuditStorageSizeReader>(ReadableStore());
        _ = services.AddSingleton(writer.Object);
        _ = services.AddSingleton<IBackgroundRequestContextWriter, TestBackgroundRequestContextWriter>();
        _ = services.AddSingleton<ILogger<PlatformBackgroundAuditRecorder>>(NullLogger<PlatformBackgroundAuditRecorder>.Instance);
        _ = services.AddSingleton<PlatformBackgroundAuditRecorder>();
        return services.BuildServiceProvider();
    }

    private static AuditStorageOutageReporter CreateReporter(RedisAuditStorageOutageCounter counter, IServiceProvider services)
    {
        return new AuditStorageOutageReporter(
            services.GetRequiredService<IServiceScopeFactory>(),
            counter,
            services.GetRequiredService<PlatformBackgroundAuditRecorder>(),
            NullLogger<AuditStorageOutageReporter>.Instance);
    }

    private static IAuditStorageSizeReader ReadableStore()
    {
        Mock<IAuditStorageSizeReader> probe = new();
        probe.Setup(reader => reader.GetStorageSizeBytesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1024);
        return probe.Object;
    }

    private sealed class TestBackgroundRequestContextWriter : IBackgroundRequestContextWriter
    {
        public void SetContext(BackgroundRequestContext context)
        {
        }
    }
}
