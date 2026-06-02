using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Domain.Entities;

/// <summary>
/// The payer-neutral clinical prescription aggregate.
/// Provider-specific mapping is generated only at submission time.
/// </summary>
public sealed class PrescriptionRecord
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string? PatientNationalId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid ClinicId { get; set; }
    public Guid? EncounterId { get; set; }

    /// <summary>Selected provider key for submission.</summary>
    public string? SelectedProviderKey { get; set; }

    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;
    public int VersionNumber { get; set; } = 1;

    /// <summary>Last recorded eligibility status from provider check.</summary>
    public EligibilityStatus? LastEligibilityStatus { get; set; }

    /// <summary>When the last eligibility check was performed.</summary>
    public DateTimeOffset? LastEligibilityCheckedAt { get; set; }

    /// <summary>Reason for cancellation (when status transitions to Cancelled).</summary>
    public string? CancellationReason { get; set; }

    /// <summary>Reason for paper fallback (when status is PaperFallback).</summary>
    public string? FallbackReason { get; set; }

    /// <summary>Paper tracking number for manual prescriptions.</summary>
    public string? PaperTrackingNumber { get; set; }

    /// <summary>Notes from support reconciliation actions.</summary>
    public string? ReconciliationNotes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>Tenant context for multi-tenant isolation.</summary>
    public string TenantId { get; set; } = string.Empty;

    // Navigation
    public List<PrescriptionItem> Items { get; set; } = [];

    /// <summary>
    /// Validates and applies a state transition. Throws if transition is illegal.
    /// </summary>
    public void TransitionTo(PrescriptionStatus newStatus)
    {
        if (!IsValidTransition(Status, newStatus))
        {
            throw new InvalidOperationException(
                $"Invalid prescription state transition from '{Status}' to '{newStatus}'.");
        }

        Status = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Checks if a state transition is valid according to the canonical lifecycle.
    /// </summary>
    public static bool IsValidTransition(PrescriptionStatus from, PrescriptionStatus to)
    {
        return (from, to) switch
        {
            (PrescriptionStatus.Draft, PrescriptionStatus.ReadyForSubmission) => true,
            (PrescriptionStatus.ReadyForSubmission, PrescriptionStatus.Submitting) => true,
            (PrescriptionStatus.Submitting, PrescriptionStatus.Submitted) => true,
            (PrescriptionStatus.Submitting, PrescriptionStatus.AcceptedByProvider) => true,
            (PrescriptionStatus.Submitting, PrescriptionStatus.RejectedByProvider) => true,
            (PrescriptionStatus.Submitting, PrescriptionStatus.SyncPending) => true,
            (PrescriptionStatus.Submitting, PrescriptionStatus.ProviderUnavailable) => true,
            (PrescriptionStatus.Submitted, PrescriptionStatus.AcceptedByProvider) => true,
            (PrescriptionStatus.RejectedByProvider, PrescriptionStatus.NeedsCorrection) => true,
            (PrescriptionStatus.NeedsCorrection, PrescriptionStatus.Draft) => true,
            (PrescriptionStatus.NeedsCorrection, PrescriptionStatus.ReadyForSubmission) => true,
            (PrescriptionStatus.AcceptedByProvider, PrescriptionStatus.EditRequested) => true,
            (PrescriptionStatus.EditRequested, PrescriptionStatus.Submitting) => true,
            (PrescriptionStatus.Edited, PrescriptionStatus.AcceptedByProvider) => true,
            (PrescriptionStatus.Submitting, PrescriptionStatus.Edited) => true,
            (PrescriptionStatus.AcceptedByProvider, PrescriptionStatus.CancelRequested) => true,
            (PrescriptionStatus.CancelRequested, PrescriptionStatus.Submitting) => true,
            (PrescriptionStatus.Submitting, PrescriptionStatus.Cancelled) => true,
            (PrescriptionStatus.SyncPending, PrescriptionStatus.AcceptedByProvider) => true,
            (PrescriptionStatus.SyncPending, PrescriptionStatus.RejectedByProvider) => true,
            (PrescriptionStatus.SyncPending, PrescriptionStatus.SyncFailed) => true,
            (PrescriptionStatus.SyncPending, PrescriptionStatus.DuplicateRisk) => true,
            (PrescriptionStatus.SyncFailed, PrescriptionStatus.ManuallyReconciled) => true,
            (PrescriptionStatus.DuplicateRisk, PrescriptionStatus.AcceptedByProvider) => true,
            (PrescriptionStatus.DuplicateRisk, PrescriptionStatus.ManuallyReconciled) => true,
            (PrescriptionStatus.ProviderUnavailable, PrescriptionStatus.PaperFallback) => true,

            // Paper fallback from any pre-submit state
            (PrescriptionStatus.Draft, PrescriptionStatus.PaperFallback) => true,
            (PrescriptionStatus.ReadyForSubmission, PrescriptionStatus.PaperFallback) => true,
            (PrescriptionStatus.ProviderUnavailable, PrescriptionStatus.SyncPending) => true,

            // Paper fallback reconciliation
            (PrescriptionStatus.PaperFallback, PrescriptionStatus.AcceptedByProvider) => true,
            (PrescriptionStatus.PaperFallback, PrescriptionStatus.ManuallyReconciled) => true,

            _ => false
        };
    }
}
