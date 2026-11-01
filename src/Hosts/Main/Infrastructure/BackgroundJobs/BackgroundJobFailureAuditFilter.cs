using Hangfire.Common;
using Hangfire.Server;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;

namespace Vitarish.Main.Infrastructure.BackgroundJobs;

/// <summary>
/// Records a security failure when a background job fails. Hangfire filters are synchronous; job workers have
/// no synchronization context, so waiting on the recorder here cannot deadlock.
/// </summary>
internal sealed class BackgroundJobFailureAuditFilter(
    PlatformBackgroundAuditRecorder recorder,
    TimeProvider timeProvider) : JobFilterAttribute, IServerFilter
{
    private static readonly TimeSpan DedupeWindow = TimeSpan.FromMinutes(1);

    public void OnPerforming(PerformingContext context)
    {
    }

    public void OnPerformed(PerformedContext context)
    {
        if (!IsFailure(context.Exception, context.ExceptionHandled))
            return;

        _ = recorder.TryRecordAsync(CreateEvent(context.BackgroundJob.Job), CancellationToken.None).GetAwaiter().GetResult();
    }

    // Cancellation is a shutdown or a deliberate stop, not a failed capability.
    internal static bool IsFailure(Exception? exception, bool handled) =>
        exception is not null && !handled && exception is not OperationCanceledException;

    internal SecurityAuditEvent CreateEvent(Job job)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        string jobName = $"{job.Type.Name}.{job.Method.Name}";

        // One record per job within the window, so retries of a failing job cannot flood the audit.
        string idempotencyKey = $"{SecurityAuditEventCatalog.SecurityFailureOperation}:{jobName}:{now.UtcTicks / DedupeWindow.Ticks}";
        return new SecurityAuditEvent
        {
            EventName = SecurityAuditEventCatalog.SecurityFailureEvent,
            EventCategory = SecurityAuditEventCatalog.SecurityFailureCategory,
            Outcome = SecurityAuditOutcome.Failed,
            ActorKind = SecurityAuditActorKind.System,
            TenantId = SecurityAuditScopeCatalog.PlatformScopeId,
            Resource = SecurityAuditEventCatalog.BackgroundJobResourcePrefix + jobName,
            Operation = SecurityAuditEventCatalog.SecurityFailureOperation,
            OccurredAt = now.UtcDateTime,
            CorrelationId = idempotencyKey,
            SourceBoundary = SecurityAuditEventCatalog.CapabilityFailureSourceBoundary,
            SourceIpKind = SecurityAuditSourceKind.System,
            RedactedReason = SecurityAuditReasonCatalog.EndpointException,
            IdempotencyKey = idempotencyKey
        };
    }
}
