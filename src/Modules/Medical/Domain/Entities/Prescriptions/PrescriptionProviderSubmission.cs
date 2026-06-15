using Vitarish.Medical.Domain.Enums.Prescriptions;

namespace Vitarish.Medical.Domain.Entities.Prescriptions;

/// <summary>
/// Records a submission attempt to an insurance provider.
/// Stores remote state separately from canonical prescription state.
/// </summary>
public sealed class PrescriptionProviderSubmission : Entity
{
    public Guid PrescriptionRecordId { get; set; }
    public required string ProviderKey { get; set; }

    /// <summary>Operation that was performed (e.g., "RegisterPrescription", "EditPrescription").</summary>
    public required string OperationName { get; set; }

    /// <summary>Remote prescription ID assigned by the provider.</summary>
    public string? RemotePrescriptionId { get; set; }

    /// <summary>Remote tracking code assigned by the provider.</summary>
    public string? RemoteTrackingCode { get; set; }

    /// <summary>Provider-native status code.</summary>
    public string? RemoteStatusCode { get; set; }

    /// <summary>Provider-native status text.</summary>
    public string? RemoteStatusText { get; set; }

    /// <summary>Submission version (increments with each edit/resubmit).</summary>
    public int SubmissionVersion { get; set; } = 1;

    public DateTime SubmittedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }

    /// <summary>Canonical status derived from provider response.</summary>
    public PrescriptionStatus CanonicalStatus { get; set; }

    /// <summary>Reference to raw response payload (for audit, not inline).</summary>
    public string? RawResponseRef { get; set; }
}
