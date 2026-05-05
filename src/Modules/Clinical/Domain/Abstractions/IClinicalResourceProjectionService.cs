using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Abstractions;

public interface IClinicalResourceProjectionService
{
    Task<ProjectionReadDto> SyncAsync(string resourceType, string resourceId, Guid? resourceLinkId = null, CancellationToken cancellationToken = default);
    Task<ProjectionReadDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectionReadDto>> ListAsync(ProjectionFilter filter, CancellationToken cancellationToken = default);
    Task ExpireAsync(Guid id, CancellationToken cancellationToken = default);
}
