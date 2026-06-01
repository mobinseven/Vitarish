using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Domain.Entities;

/// <summary>
/// Tracks a reference data sync operation (one per provider + code system execution).
/// Supports versioned import with freshness monitoring.
/// </summary>
public sealed class ReferenceCodeSyncJob
{
    public Guid Id { get; set; }

    /// <summary>Provider key this sync is for.</summary>
    public required string ProviderKey { get; set; }

    /// <summary>Code system being synced (e.g., "Drug", "Service").</summary>
    public required string CodeSystem { get; set; }

    public ReferenceCodeSyncStatus Status { get; set; } = ReferenceCodeSyncStatus.Pending;

    /// <summary>Version identifier returned by the provider for this sync.</summary>
    public string? DataVersion { get; set; }

    /// <summary>Previous version before this sync (for delta tracking).</summary>
    public string? PreviousDataVersion { get; set; }

    public int TotalItemsProcessed { get; set; }
    public int ItemsAdded { get; set; }
    public int ItemsUpdated { get; set; }
    public int ItemsDeactivated { get; set; }
    public int ErrorCount { get; set; }

    /// <summary>Duration in milliseconds.</summary>
    public long DurationMs { get; set; }

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Last error message if status is Failed or CompletedWithErrors.</summary>
    public string? LastError { get; set; }

    /// <summary>Whether this was triggered manually by an admin.</summary>
    public bool IsManualTrigger { get; set; }

    /// <summary>Who or what triggered this sync (e.g., "hangfire:schedule", "admin:userId").</summary>
    public string? TriggeredBy { get; set; }

    public SyncPriority Priority { get; set; } = SyncPriority.Normal;

    /// <summary>Tenant scope for this sync job.</summary>
    public string? TenantId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
