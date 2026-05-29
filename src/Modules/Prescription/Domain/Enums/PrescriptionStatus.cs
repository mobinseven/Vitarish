namespace Vitarish.Prescription.Domain.Enums;

/// <summary>
/// Canonical prescription lifecycle states.
/// Provider-native statuses are stored separately — this is the platform truth.
/// </summary>
public enum PrescriptionStatus
{
    Draft,
    ReadyForSubmission,
    Submitting,
    Submitted,
    AcceptedByProvider,
    RejectedByProvider,
    NeedsCorrection,
    EditRequested,
    Edited,
    CancelRequested,
    Cancelled,
    PaperFallback,
    SyncPending,
    SyncFailed,
    DuplicateRisk,
    ProviderUnavailable,
    ManuallyReconciled
}
