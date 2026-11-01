using FluentAssertions;
using Hangfire.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Vitarish.Audit.Domain;
using Vitarish.Main.Infrastructure.BackgroundJobs;
using Xunit;

namespace Main.Tests;

public sealed class BackgroundJobFailureAuditFilterTests
{
    private static readonly Job FailingJob = Job.FromExpression(() => SampleJob.Run());

    [Fact]
    public void Only_an_unhandled_failure_counts_as_a_failed_capability()
    {
        BackgroundJobFailureAuditFilter.IsFailure(new InvalidOperationException(), handled: false).Should().BeTrue();
        BackgroundJobFailureAuditFilter.IsFailure(null, handled: false).Should().BeFalse();
        BackgroundJobFailureAuditFilter.IsFailure(new InvalidOperationException(), handled: true).Should().BeFalse();
        BackgroundJobFailureAuditFilter.IsFailure(new OperationCanceledException(), handled: false).Should().BeFalse();
    }

    [Fact]
    public void A_failed_job_is_recorded_as_a_system_failure_naming_only_the_job()
    {
        SecurityAuditEvent auditEvent = CreateFilter(new FixedTime(new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero))).CreateEvent(FailingJob);

        auditEvent.EventName.Should().Be(SecurityAuditEventCatalog.SecurityFailureEvent);
        auditEvent.Outcome.Should().Be(SecurityAuditOutcome.Failed);
        auditEvent.ActorKind.Should().Be(SecurityAuditActorKind.System);
        auditEvent.SourceIpKind.Should().Be(SecurityAuditSourceKind.System);
        auditEvent.TenantId.Should().Be(SecurityAuditScopeCatalog.PlatformScopeId);
        auditEvent.Resource.Should().Be(SecurityAuditEventCatalog.BackgroundJobResourcePrefix + "SampleJob.Run");
        auditEvent.RedactedReason.Should().Be(SecurityAuditReasonCatalog.EndpointException);
    }

    [Fact]
    public void Retries_inside_one_minute_share_a_key_and_a_later_failure_does_not()
    {
        DateTimeOffset start = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

        string first = CreateFilter(new FixedTime(start)).CreateEvent(FailingJob).IdempotencyKey!;
        string retry = CreateFilter(new FixedTime(start.AddSeconds(30))).CreateEvent(FailingJob).IdempotencyKey!;
        string later = CreateFilter(new FixedTime(start.AddMinutes(1))).CreateEvent(FailingJob).IdempotencyKey!;

        retry.Should().Be(first);
        later.Should().NotBe(first);
    }

    private static BackgroundJobFailureAuditFilter CreateFilter(TimeProvider time) =>
        new(
            new PlatformBackgroundAuditRecorder(
                new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
                NullLogger<PlatformBackgroundAuditRecorder>.Instance),
            time);

    public static class SampleJob
    {
        public static void Run()
        {
        }
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
