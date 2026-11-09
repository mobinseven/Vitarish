using MediatR;

namespace Vitarish.Medical.Application.Commands.Prescriptions;

/// <summary>Triggers a reference data sync for a specific provider and code system.</summary>
public sealed record SyncReferenceDataCommand : IRequest<SyncReferenceDataResult>
{
    public required string ProviderKey { get; init; }
    public required string CodeSystem { get; init; }
    public string? TriggeredBy { get; init; }
}

public sealed record SyncReferenceDataResult
{
    public bool IsSuccess { get; init; }
    public Guid? ImportBatchId { get; init; }
    public int? TotalItemsProcessed { get; init; }
    public string? ErrorMessage { get; init; }

    public static SyncReferenceDataResult Success(Guid importBatchId, int totalItems) => new()
    {
        IsSuccess = true,
        ImportBatchId = importBatchId,
        TotalItemsProcessed = totalItems
    };

    public static SyncReferenceDataResult Failure(string error) => new()
    {
        IsSuccess = false,
        ErrorMessage = error
    };
}
