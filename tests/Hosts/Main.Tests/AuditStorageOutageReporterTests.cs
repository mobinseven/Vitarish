using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Main.Infrastructure.BackgroundJobs;
using Vitarish.Modules.Base.Core.BackgroundJobs;

namespace Main.Tests;

public sealed class AuditStorageOutageReporterTests
{
    private static readonly DateTime Started = new(2026, 10, 25, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime LastFailed = new(2026, 10, 25, 9, 42, 5, DateTimeKind.Utc);

    [Fact]
    public async Task Without_an_outage_nothing_is_probed_or_written()
    {
        TestOutageCounter counter = new();
        Mock<IAuditStorageSizeReader> probe = new();
        Mock<ISecurityAuditWriter> writer = CreateWriter(SecurityAuditWriteResult.Recorded);

        await CreateReporter(counter, probe.Object, writer).CheckOnceAsync(CancellationToken.None);

        probe.VerifyNoOtherCalls();
        writer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task While_the_store_is_still_unreadable_the_outage_is_kept_and_nothing_is_written()
    {
        TestOutageCounter counter = new(new AuditStorageOutage(Started, LastFailed, 3));
        Mock<IAuditStorageSizeReader> probe = new();
        probe.Setup(reader => reader.GetStorageSizeBytesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("store down"));
        Mock<ISecurityAuditWriter> writer = CreateWriter(SecurityAuditWriteResult.Recorded);

        await CreateReporter(counter, probe.Object, writer).CheckOnceAsync(CancellationToken.None);

        writer.VerifyNoOtherCalls();
        (await counter.GetAsync(CancellationToken.None)).Should().NotBeNull();
    }

    [Fact]
    public async Task After_recovery_one_platform_security_failure_summarizes_the_outage_and_clears_it()
    {
        TestOutageCounter counter = new(new AuditStorageOutage(Started, LastFailed, 37));
        Mock<ISecurityAuditWriter> writer = CreateWriter(SecurityAuditWriteResult.Recorded);
        AuditStorageOutageReporter reporter = CreateReporter(counter, ReadableStore(), writer);

        await reporter.CheckOnceAsync(CancellationToken.None);
        await reporter.CheckOnceAsync(CancellationToken.None);

        SecurityAuditEvent summary = Written(writer).Should().ContainSingle().Subject;
        summary.EventName.Should().Be(SecurityAuditEventCatalog.SecurityFailureEvent);
        summary.Outcome.Should().Be(SecurityAuditOutcome.Failed);
        summary.ActorKind.Should().Be(SecurityAuditActorKind.System);
        summary.TenantId.Should().Be(SecurityAuditScopeCatalog.PlatformScopeId);
        summary.RedactedReason.Should().Be(SecurityAuditReasonCatalog.AuditStorageOutage(Started, LastFailed, 37));
        (await counter.GetAsync(CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task A_summary_already_recorded_before_a_crash_is_not_written_twice_and_the_outage_is_cleared()
    {
        TestOutageCounter counter = new(new AuditStorageOutage(Started, LastFailed, 2));
        Mock<ISecurityAuditWriter> writer = CreateWriter(SecurityAuditWriteResult.AlreadyRecorded);

        await CreateReporter(counter, ReadableStore(), writer).CheckOnceAsync(CancellationToken.None);

        (await counter.GetAsync(CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task A_failure_between_reading_and_clearing_the_outage_still_records_one_summary()
    {
        TestOutageCounter counter = new(new AuditStorageOutage(Started, LastFailed, 2));
        HashSet<string> recordedKeys = [];
        bool failedWriteRecorded = false;
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(item => item.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SecurityAuditEvent auditEvent, CancellationToken _) =>
            {
                if (!failedWriteRecorded)
                {
                    failedWriteRecorded = true;
                    counter.RecordFailedWriteAsync(LastFailed.AddSeconds(1), CancellationToken.None).GetAwaiter().GetResult();
                }

                return recordedKeys.Add(auditEvent.IdempotencyKey)
                    ? SecurityAuditWriteResult.Recorded
                    : SecurityAuditWriteResult.AlreadyRecorded;
            });
        AuditStorageOutageReporter reporter = CreateReporter(counter, ReadableStore(), writer);

        await reporter.CheckOnceAsync(CancellationToken.None);
        (await counter.GetAsync(CancellationToken.None)).Should()
            .NotBeNull("the failure after the read keeps the outage open");

        await reporter.CheckOnceAsync(CancellationToken.None);

        (await counter.GetAsync(CancellationToken.None)).Should().BeNull();
        Written(writer).Select(item => item.IdempotencyKey).Distinct().Should().ContainSingle();
    }

    private static AuditStorageOutageReporter CreateReporter(
        TestOutageCounter counter,
        IAuditStorageSizeReader probe,
        Mock<ISecurityAuditWriter> writer)
    {
        ServiceCollection services = new();
        _ = services.AddSingleton(probe);
        _ = services.AddSingleton(writer.Object);
        _ = services.AddSingleton<IBackgroundRequestContextWriter, TestBackgroundRequestContextWriter>();
        _ = services.AddSingleton<ILogger<PlatformBackgroundAuditRecorder>>(NullLogger<PlatformBackgroundAuditRecorder>.Instance);
        _ = services.AddSingleton<PlatformBackgroundAuditRecorder>();
        ServiceProvider provider = services.BuildServiceProvider();
        return new AuditStorageOutageReporter(
            provider.GetRequiredService<IServiceScopeFactory>(),
            counter,
            provider.GetRequiredService<PlatformBackgroundAuditRecorder>(),
            NullLogger<AuditStorageOutageReporter>.Instance);
    }

    private static IAuditStorageSizeReader ReadableStore()
    {
        Mock<IAuditStorageSizeReader> probe = new();
        probe.Setup(reader => reader.GetStorageSizeBytesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1024);
        return probe.Object;
    }

    private static Mock<ISecurityAuditWriter> CreateWriter(SecurityAuditWriteResult result)
    {
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(item => item.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        return writer;
    }

    private static SecurityAuditEvent[] Written(Mock<ISecurityAuditWriter> writer) => writer.Invocations
        .Where(invocation => invocation.Method.Name == nameof(ISecurityAuditWriter.WriteAsync))
        .Select(invocation => (SecurityAuditEvent)invocation.Arguments[0])
        .ToArray();

    private sealed class TestBackgroundRequestContextWriter : IBackgroundRequestContextWriter
    {
        public void SetContext(BackgroundRequestContext context)
        {
        }
    }

    private sealed class TestOutageCounter(AuditStorageOutage? outage = null) : IAuditStorageOutageCounter
    {
        private AuditStorageOutage? current = outage;

        public Task RecordFailedWriteAsync(DateTime failedAt, CancellationToken cancellationToken)
        {
            current = current is null
                ? new AuditStorageOutage(failedAt, failedAt, 1)
                : current with { LastFailedAt = failedAt, FailedWrites = current.FailedWrites + 1 };
            return Task.CompletedTask;
        }

        public Task<AuditStorageOutage?> GetAsync(CancellationToken cancellationToken) => Task.FromResult(current);

        public Task<bool> ClearAsync(AuditStorageOutage summarized, CancellationToken cancellationToken)
        {
            if (current != summarized)
                return Task.FromResult(false);
            current = null;
            return Task.FromResult(true);
        }
    }
}
