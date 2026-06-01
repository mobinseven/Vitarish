using MediatR;
using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Application.Queries;

/// <summary>
/// Gets prescriptions requiring support attention (SyncPending, SyncFailed, DuplicateRisk, PaperFallback, ProviderUnavailable).
/// Used by the support reconciliation desk.
/// </summary>
public sealed record GetReconciliationQueueQuery : IRequest<ReconciliationQueueResult>
{
    public string? TenantId { get; init; }
    public string? ProviderKey { get; init; }
    public PrescriptionStatus? StatusFilter { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 50;
}

public sealed record ReconciliationQueueResult
{
    public IReadOnlyList<ReconciliationItemDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
}

public sealed record ReconciliationItemDto
{
    public Guid PrescriptionId { get; init; }
    public Guid PatientId { get; init; }
    public string? PatientNationalId { get; init; }
    public Guid DoctorId { get; init; }
    public Guid ClinicId { get; init; }
    public PrescriptionStatus Status { get; init; }
    public string? SelectedProviderKey { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? SubmittedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public int RetryCount { get; init; }
    public string? LastError { get; init; }
    public string? RemotePrescriptionId { get; init; }
    public string? PaperTrackingNumber { get; init; }
    public string? CorrelationId { get; init; }
    public string? RequestHash { get; init; }
    public string? IdempotencyKey { get; init; }
    public DateTimeOffset? LastAttemptAt { get; init; }
}

/// <summary>
/// Gets outbox entries for monitoring retry queue depth and status.
/// </summary>
public sealed record GetOutboxStatusQuery : IRequest<OutboxStatusResult>
{
    public string? ProviderKey { get; init; }
    public string? TenantId { get; init; }
}

public sealed record OutboxStatusResult
{
    public int PendingCount { get; init; }
    public int ProcessingCount { get; init; }
    public int FailedCount { get; init; }
    public int DuplicateRiskCount { get; init; }
    public IReadOnlyList<OutboxEntryDto> RecentEntries { get; init; } = [];
}

public sealed record OutboxEntryDto
{
    public Guid Id { get; init; }
    public Guid? PrescriptionId { get; init; }
    public string ProviderKey { get; init; } = string.Empty;
    public string OperationName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int RetryCount { get; init; }
    public int MaxRetries { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? NextRetryAt { get; init; }
    public DateTimeOffset? LastAttemptAt { get; init; }
    public string? LastError { get; init; }
}
