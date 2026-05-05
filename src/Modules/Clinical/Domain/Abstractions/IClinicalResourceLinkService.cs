using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Abstractions;

public interface IClinicalResourceLinkService
{
    Task<ResourceLinkReadDto> LinkAsync(ResourceLinkWriteDto writeDto, CancellationToken cancellationToken = default);
    Task UnlinkAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResourceLinkReadDto>> GetLinksAsync(ResourceLinkFilter filter, CancellationToken cancellationToken = default);
    Task<ResourceLinkReadDto?> GetLinkAsync(Guid id, CancellationToken cancellationToken = default);
}
