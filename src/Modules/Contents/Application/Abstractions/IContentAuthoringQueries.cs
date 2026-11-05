using Vitarish.Contents.Application.Models;
using Vitarish.Contents.Domain.Enums;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Application.Abstractions;

public interface IContentAuthoringQueries
{
    Task<EntityResult<IReadOnlyList<SiteReadModel>>> ListSitesAsync(CancellationToken cancellationToken);

    Task<EntityResult<IReadOnlyList<ContentTypeReadModel>>> ListContentTypesAsync(CancellationToken cancellationToken);

    Task<EntityResult<IReadOnlyList<ContentEntryReadModel>>> ListEntriesAsync(
        Guid siteId,
        Guid? typeId,
        ContentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<EntityResult<ContentEntryReadModel>> GetEntryAsync(Guid contentId, CancellationToken cancellationToken);
}
