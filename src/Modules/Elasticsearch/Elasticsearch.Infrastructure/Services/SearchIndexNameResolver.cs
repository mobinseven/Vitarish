using Microsoft.Extensions.Options;
using Vitarish.Modules.Elasticsearch.Application.Abstractions;
using Vitarish.Modules.Elasticsearch.Infrastructure.Configuration;

namespace Vitarish.Modules.Elasticsearch.Infrastructure.Services;

public class SearchIndexNameResolver(IOptions<ElasticsearchOptions> options) : ISearchIndexNameResolver
{
    private readonly ElasticsearchOptions _options = options.Value;

    public string ResolveIndexName(string entityType, string? tenantId = null)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException("Entity type is required.", nameof(entityType));
        }

        var normalizedEntityType = entityType.Trim().ToLowerInvariant();
        var normalizedTenant = string.IsNullOrWhiteSpace(tenantId)
            ? null
            : tenantId.Trim().ToLowerInvariant();

        return normalizedTenant is null
            ? $"{_options.IndexPrefix}-{normalizedEntityType}"
            : $"{_options.IndexPrefix}-{normalizedTenant}-{normalizedEntityType}";
    }
}
