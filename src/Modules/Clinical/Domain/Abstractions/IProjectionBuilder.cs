using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Abstractions;

public interface IProjectionBuilder
{
    Task RefreshNurseQueueAsync(CancellationToken cancellationToken = default);
    Task RefreshProviderWorklistAsync(CancellationToken cancellationToken = default);
    Task RefreshIncompleteVisitsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OperationalProjectionSummary>> GetQueueAsync(string projectionType, string? locationId = null, CancellationToken cancellationToken = default);
}
