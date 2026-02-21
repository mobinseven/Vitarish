using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Logging;
using Vitarish.Modules.Elasticsearch.Application.Abstractions;
using Vitarish.Modules.Elasticsearch.Application.Models;

namespace Vitarish.Modules.Elasticsearch.Infrastructure.Services;

public class ElasticsearchIndexService(
    ElasticsearchClient client,
    ISearchIndexNameResolver indexNameResolver,
    ILogger<ElasticsearchIndexService> logger) : ISearchIndexService
{
    public async Task IndexDocumentAsync(IndexDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var indexName = indexNameResolver.ResolveIndexName(request.EntityType, request.TenantId);
        var documentId = BuildDocumentId(request.TenantId, request.EntityType, request.EntityId);

        var response = await client.IndexAsync(
            request.Payload,
            descriptor => descriptor
                .Index(indexName)
                .Id(documentId),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            logger.LogError("Failed to index document {EntityType}/{EntityId} into index {Index}.", request.EntityType, request.EntityId, indexName);
            throw new InvalidOperationException($"Failed to index document '{request.EntityType}/{request.EntityId}'.");
        }
    }

    public async Task DeleteDocumentAsync(string entityType, string entityId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var indexName = indexNameResolver.ResolveIndexName(entityType, tenantId);
        var documentId = BuildDocumentId(tenantId, entityType, entityId);
        var response = await client.DeleteAsync<object>(documentId, descriptor => descriptor.Index(indexName), cancellationToken);

        if (!response.IsValidResponse)
        {
            logger.LogError("Failed to delete document {EntityType}/{EntityId} from index {Index}.", entityType, entityId, indexName);
            throw new InvalidOperationException($"Failed to delete document '{entityType}/{entityId}'.");
        }
    }

    private static string BuildDocumentId(string? tenantId, string entityType, string entityId)
    {
        var normalizedTenant = string.IsNullOrWhiteSpace(tenantId) ? "global" : tenantId.Trim();
        return $"{normalizedTenant}:{entityType}:{entityId}";
    }
}
