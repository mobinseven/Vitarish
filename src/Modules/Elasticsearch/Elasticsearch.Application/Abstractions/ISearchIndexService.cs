using Vitarish.Modules.Elasticsearch.Application.Models;

namespace Vitarish.Modules.Elasticsearch.Application.Abstractions;

public interface ISearchIndexService
{
    Task IndexDocumentAsync(IndexDocumentRequest request, CancellationToken cancellationToken = default);

    Task DeleteDocumentAsync(string entityType, string entityId, string? tenantId = null, CancellationToken cancellationToken = default);
}
