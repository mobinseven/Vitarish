using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Domain.Entities;

/// <summary>
/// Records changes detected during a reference data sync for audit and breaking-change detection.
/// </summary>
public sealed class ReferenceCodeChangeLog
{
    public Guid Id { get; set; }
    public Guid SyncJobId { get; set; }

    /// <summary>Reference to the affected code entry (if still exists).</summary>
    public Guid? ReferenceCodeId { get; set; }

    public ReferenceCodeChangeType ChangeType { get; set; }

    /// <summary>The code value that changed.</summary>
    public required string Code { get; set; }

    /// <summary>Previous value (for updates) or previous status (for deactivation).</summary>
    public string? OldValue { get; set; }

    /// <summary>New value after the change.</summary>
    public string? NewValue { get; set; }

    /// <summary>Whether this change breaks existing crosswalk mappings.</summary>
    public bool IsBreakingChange { get; set; }

    /// <summary>Number of crosswalk mappings affected (if breaking).</summary>
    public int AffectedCrosswalkCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
