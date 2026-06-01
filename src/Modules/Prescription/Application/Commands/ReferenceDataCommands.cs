using MediatR;
using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Application.Commands;

/// <summary>Triggers a reference data sync for a specific provider and code system.</summary>
public sealed record SyncReferenceDataCommand : IRequest<SyncReferenceDataResult>
{
    public required string ProviderKey { get; init; }
    public required string CodeSystem { get; init; }
    public SyncPriority Priority { get; init; } = SyncPriority.Normal;
    public string? TriggeredBy { get; init; }
}

public sealed record SyncReferenceDataResult
{
    public bool IsSuccess { get; init; }
    public Guid? SyncJobId { get; init; }
    public ReferenceCodeSyncStatus? Status { get; init; }
    public int? TotalItemsProcessed { get; init; }
    public (int Added, int Updated, int Deactivated)? Changes { get; init; }
    public string? ErrorMessage { get; init; }

    public static SyncReferenceDataResult Success(
        Guid jobId, ReferenceCodeSyncStatus status, int totalItems, (int, int, int) changes) => new()
    {
        IsSuccess = true,
        SyncJobId = jobId,
        Status = status,
        TotalItemsProcessed = totalItems,
        Changes = changes
    };

    public static SyncReferenceDataResult Failure(string error) => new()
    {
        IsSuccess = false,
        ErrorMessage = error
    };
}
