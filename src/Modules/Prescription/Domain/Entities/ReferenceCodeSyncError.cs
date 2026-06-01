namespace Vitarish.Prescription.Domain.Entities;

/// <summary>
/// Individual error encountered during a reference data sync operation.
/// </summary>
public sealed class ReferenceCodeSyncError
{
    public Guid Id { get; set; }
    public Guid SyncJobId { get; set; }

    /// <summary>Code that failed to process.</summary>
    public string? Code { get; set; }

    /// <summary>Error message.</summary>
    public required string ErrorMessage { get; set; }

    /// <summary>Whether this error is recoverable on retry.</summary>
    public bool IsRecoverable { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
