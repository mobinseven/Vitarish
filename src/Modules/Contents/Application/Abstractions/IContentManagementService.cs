using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Application.Abstractions;

public interface IContentManagementService
{
    Task<EntityResult<IReadOnlyList<ContentItemReadDto>>> ListAsync(CancellationToken cancellationToken);
    Task<EntityResult<ContentItemReadDto>> GetAsync(Guid contentId, CancellationToken cancellationToken);
    Task<EntityResult<ContentItemReadDto>> CreateAsync(ContentItemWriteDto item, CancellationToken cancellationToken);
    Task<EntityResult<ContentItemReadDto>> UpdateAsync(Guid contentId, ContentItemWriteDto item, CancellationToken cancellationToken);
    Task<EntityResult<ContentItemReadDto>> ArchiveAsync(Guid contentId, CancellationToken cancellationToken);
    Task<EntityResult<IReadOnlyList<ContentRevisionReadDto>>> ListRevisionsAsync(Guid contentId, CancellationToken cancellationToken);
}
