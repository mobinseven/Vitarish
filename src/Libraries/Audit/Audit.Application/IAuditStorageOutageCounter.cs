namespace Vitarish.Audit.Application;

/// <summary>
/// Durably counts security audit writes that the store refused, outside the audit store itself, so that
/// one summary of the outage can be recorded once the store accepts writes again. It holds no event
/// content: only when the outage started, when a write last failed, and how many writes failed.
/// </summary>
public interface IAuditStorageOutageCounter
{
    Task RecordFailedWriteAsync(DateTime failedAt, CancellationToken cancellationToken);

    /// <summary>The outage not yet summarized, or null when no write has failed since the last summary.</summary>
    Task<AuditStorageOutage?> GetAsync(CancellationToken cancellationToken);

    /// <summary>Reserves a stable snapshot after a healthy probe. Later failures belong to the next outage interval.</summary>
    Task<AuditStorageOutage?> ReserveSummaryAsync(CancellationToken cancellationToken);

    /// <summary>Clears only the reserved snapshot after its summary is durably recorded.</summary>
    Task<bool> ClearAsync(AuditStorageOutage summarized, CancellationToken cancellationToken);
}

public sealed record AuditStorageOutage(DateTime StartedAt, DateTime LastFailedAt, long FailedWrites);
