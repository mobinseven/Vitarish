using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vitarish.Audit.Application;

namespace Vitarish.E2e.Infrastructure.Audit;

/// <summary>
/// Makes every audit-store save fail while an outage is simulated, so the real security audit writer
/// takes its failure path. It is switched on and off only by the audit-evidence endpoints.
/// </summary>
internal sealed class E2eAuditStoreOutageSimulation : SaveChangesInterceptor
{
    private volatile bool unavailable;

    public bool Unavailable
    {
        get => unavailable;
        set => unavailable = value;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default) =>
        unavailable
            ? throw new DbUpdateException("The E2E audit store is simulating an outage.")
            : ValueTask.FromResult(result);

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) =>
        unavailable ? throw new DbUpdateException("The E2E audit store is simulating an outage.") : result;
}

/// <summary>The E2E host has no Redis; its outage counter lives as long as the host process.</summary>
internal sealed class E2eAuditStorageOutageCounter : IAuditStorageOutageCounter
{
    private readonly Lock gate = new();
    private AuditStorageOutage? outage;

    public Task RecordFailedWriteAsync(DateTime failedAt, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            outage = outage is null
                ? new AuditStorageOutage(failedAt, failedAt, 1)
                : outage with
                {
                    LastFailedAt = failedAt > outage.LastFailedAt ? failedAt : outage.LastFailedAt,
                    FailedWrites = outage.FailedWrites + 1
                };
        }
        return Task.CompletedTask;
    }

    public Task<AuditStorageOutage?> GetAsync(CancellationToken cancellationToken)
    {
        lock (gate)
            return Task.FromResult(outage);
    }

    public Task<bool> ClearAsync(AuditStorageOutage summarized, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            if (outage != summarized)
                return Task.FromResult(false);
            outage = null;
            return Task.FromResult(true);
        }
    }
}
