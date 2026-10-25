using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Modules.Base.Core.BackgroundJobs;

namespace Vitarish.Main.Infrastructure.BackgroundJobs;

/// <summary>
/// Records one security-failure event per audit-store outage once the store accepts writes again: when
/// writes started failing, when the last one failed, and how many failed. The refused events themselves
/// are not kept; the outage is visible in the audit log instead of only in the host error log.
/// </summary>
internal sealed class AuditStorageOutageReporter(
    IServiceScopeFactory scopeFactory,
    IAuditStorageOutageCounter outageCounter,
    ILogger<AuditStorageOutageReporter> logger) : BackgroundService
{
    internal static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(CheckInterval);

        do
        {
            await CheckOnceAsync(stoppingToken).ConfigureAwait(false);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    internal async Task CheckOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            AuditStorageOutage? outage = await outageCounter.GetAsync(cancellationToken).ConfigureAwait(false);
            if (outage is null)
                return;

            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(OperationTimeout);

            // Read first: while the store is still unreachable, writing the summary would only count
            // as one more failed write of the outage it describes.
            _ = await scope.ServiceProvider.GetRequiredService<IAuditStorageSizeReader>()
                .GetStorageSizeBytesAsync(timeout.Token).ConfigureAwait(false);

            SecurityAuditEvent summary = CreateSummaryEvent(outage);
            scope.ServiceProvider.GetRequiredService<IBackgroundRequestContextWriter>().SetContext(new BackgroundRequestContext(
                TenantBackgroundExecutor.BackgroundAuthority,
                ClientId: null,
                OwnerId: null,
                TenantId: SecurityAuditScopeCatalog.PlatformScopeId,
                ResourceId: summary.Resource,
                RequestId: summary.IdempotencyKey));
            _ = await scope.ServiceProvider.GetRequiredService<ISecurityAuditWriter>()
                .WriteAsync(summary, timeout.Token).ConfigureAwait(false);

            // A write that failed after the outage was read keeps it open; the next check records the
            // larger count under its own key.
            _ = await outageCounter.ClearAsync(outage, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError("Audit storage outage summary not recorded yet ({FailureType}).", exception.GetType().Name);
        }
    }

    private static SecurityAuditEvent CreateSummaryEvent(AuditStorageOutage outage)
    {
        // One key per outage and count, so a retry after a crash finds the summary already recorded.
        string idempotencyKey = $"{SecurityAuditEventCatalog.SecurityFailureOperation}:{SecurityAuditEventCatalog.AuditStorageOutageSourceBoundary}:{outage.StartedAt.Ticks}:{outage.FailedWrites}";
        return new SecurityAuditEvent
        {
            EventName = SecurityAuditEventCatalog.SecurityFailureEvent,
            EventCategory = SecurityAuditEventCatalog.SecurityFailureCategory,
            Outcome = SecurityAuditOutcome.Failed,
            ActorKind = SecurityAuditActorKind.System,
            TenantId = SecurityAuditScopeCatalog.PlatformScopeId,
            Resource = SecurityAuditScopeCatalog.PlatformResource,
            Operation = SecurityAuditEventCatalog.SecurityFailureOperation,
            OccurredAt = DateTime.UtcNow,
            CorrelationId = idempotencyKey,
            SourceBoundary = SecurityAuditEventCatalog.AuditStorageOutageSourceBoundary,
            SourceIpKind = SecurityAuditSourceKind.System,
            RedactedReason = SecurityAuditReasonCatalog.AuditStorageOutage(outage.StartedAt, outage.LastFailedAt, outage.FailedWrites),
            IdempotencyKey = idempotencyKey
        };
    }
}
