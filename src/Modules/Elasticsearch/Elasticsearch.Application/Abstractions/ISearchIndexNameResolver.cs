namespace Vitarish.Modules.Elasticsearch.Application.Abstractions;

public interface ISearchIndexNameResolver
{
    string ResolveIndexName(string entityType, string? tenantId = null);
}
