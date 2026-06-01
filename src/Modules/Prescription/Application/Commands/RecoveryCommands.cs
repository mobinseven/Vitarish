using MediatR;
using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Application.Commands;

/// <summary>
/// Transitions a prescription to PaperFallback state with reason capture and paper tracking number.
/// Allowed from Draft, ReadyForSubmission, or ProviderUnavailable states.
/// </summary>
public sealed record PaperFallbackCommand : IRequest<PaperFallbackResult>
{
    public required Guid PrescriptionId { get; init; }
    public required string Reason { get; init; }
    public string? PaperTrackingNumber { get; init; }
    public required string CorrelationId { get; init; }
}

public sealed record PaperFallbackResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static PaperFallbackResult Success() => new() { IsSuccess = true };
    public static PaperFallbackResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>
/// Resolves a DuplicateRisk prescription by confirming the final provider state.
/// Support operator provides whether the prescription was actually accepted or should be marked reconciled.
/// </summary>
public sealed record ResolveDuplicateRiskCommand : IRequest<ResolveDuplicateRiskResult>
{
    public required Guid PrescriptionId { get; init; }
    public required DuplicateRiskResolution Resolution { get; init; }
    public string? Notes { get; init; }
    public required string CorrelationId { get; init; }
}

public enum DuplicateRiskResolution
{
    /// <summary>Confirmed that provider accepted the prescription.</summary>
    ConfirmedAccepted,
    /// <summary>Confirmed that provider rejected/never received; mark as manually reconciled.</summary>
    ConfirmedNotSubmitted
}

public sealed record ResolveDuplicateRiskResult
{
    public bool IsSuccess { get; init; }
    public PrescriptionStatus ResultStatus { get; init; }
    public string? ErrorMessage { get; init; }

    public static ResolveDuplicateRiskResult Success(PrescriptionStatus status) => new() { IsSuccess = true, ResultStatus = status };
    public static ResolveDuplicateRiskResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>
/// Manually reconciles a prescription that is in SyncFailed, DuplicateRisk, or PaperFallback state.
/// Used by support operators for final state correction.
/// </summary>
public sealed record ManualReconcileCommand : IRequest<ManualReconcileResult>
{
    public required Guid PrescriptionId { get; init; }
    public required string Notes { get; init; }
    public required string CorrelationId { get; init; }
}

public sealed record ManualReconcileResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static ManualReconcileResult Success() => new() { IsSuccess = true };
    public static ManualReconcileResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>
/// Refreshes the provider-side status of a prescription by fetching from the remote system.
/// Used for SyncPending/DuplicateRisk prescriptions to check if the provider has an answer.
/// </summary>
public sealed record RefreshProviderStatusCommand : IRequest<RefreshProviderStatusResult>
{
    public required Guid PrescriptionId { get; init; }
    public required string CorrelationId { get; init; }
}

public sealed record RefreshProviderStatusResult
{
    public bool IsSuccess { get; init; }
    public PrescriptionStatus? NewStatus { get; init; }
    public string? ProviderStatusText { get; init; }
    public string? ErrorMessage { get; init; }

    public static RefreshProviderStatusResult Success(PrescriptionStatus status, string? statusText)
        => new() { IsSuccess = true, NewStatus = status, ProviderStatusText = statusText };
    public static RefreshProviderStatusResult Failure(string error)
        => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>
/// Reconciles a PaperFallback prescription back into the electronic system after provider recovery.
/// </summary>
public sealed record ReconcilePaperFallbackCommand : IRequest<ReconcilePaperFallbackResult>
{
    public required Guid PrescriptionId { get; init; }
    public required string CorrelationId { get; init; }
    /// <summary>If the prescription was later submitted electronically, its remote ID.</summary>
    public string? RemotePrescriptionId { get; init; }
}

public sealed record ReconcilePaperFallbackResult
{
    public bool IsSuccess { get; init; }
    public PrescriptionStatus ResultStatus { get; init; }
    public string? ErrorMessage { get; init; }

    public static ReconcilePaperFallbackResult Success(PrescriptionStatus status) => new() { IsSuccess = true, ResultStatus = status };
    public static ReconcilePaperFallbackResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>
/// Escalates a prescription from the reconciliation queue for higher-level review.
/// Creates an escalation record and removes the prescription from the primary queue.
/// </summary>
public sealed record EscalateCommand : IRequest<EscalateResult>
{
    public required Guid PrescriptionId { get; init; }
    public required Guid OperatorId { get; init; }
    public required string Summary { get; init; }
    public required string CorrelationId { get; init; }
}

public sealed record EscalateResult
{
    public bool IsSuccess { get; init; }
    public Guid? EscalationId { get; init; }
    public string? ErrorMessage { get; init; }

    public static EscalateResult Success(Guid id) => new() { IsSuccess = true, EscalationId = id };
    public static EscalateResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}
