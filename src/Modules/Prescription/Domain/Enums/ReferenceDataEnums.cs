namespace Vitarish.Prescription.Domain.Enums;

/// <summary>
/// Status of a reference data sync job.
/// </summary>
public enum ReferenceCodeSyncStatus
{
    Pending,
    InProgress,
    Completed,
    CompletedWithErrors,
    Failed,
    Cancelled
}

/// <summary>
/// Type of change detected during reference data sync.
/// </summary>
public enum ReferenceCodeChangeType
{
    Added,
    Updated,
    Deactivated,
    Reactivated
}

/// <summary>
/// Priority level for sync jobs.
/// </summary>
public enum SyncPriority
{
    Low,
    Normal,
    High
}
