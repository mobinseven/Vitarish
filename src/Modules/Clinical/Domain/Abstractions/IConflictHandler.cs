using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Abstractions;

public interface IConflictHandler
{
    Task<bool> IsDuplicateActionAsync(Guid workflowId, ClinicalActionType actionType, TimeSpan? window = null, CancellationToken cancellationToken = default);
    Task<bool> IsStaleVersionAsync(string resourceType, string resourceId, string? expectedVersionId, CancellationToken cancellationToken = default);
    bool IsRetriableSafe(ClinicalActionType actionType);
}
