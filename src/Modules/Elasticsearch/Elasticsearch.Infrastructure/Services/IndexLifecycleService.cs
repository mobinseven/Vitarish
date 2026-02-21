using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Microsoft.Extensions.Logging;
using Vitarish.Modules.Elasticsearch.Application.Models;

namespace Vitarish.Modules.Elasticsearch.Infrastructure.Services;

public class IndexLifecycleService(
    ElasticsearchClient client,
    ILogger<IndexLifecycleService> logger)
{
    public async Task<string> EnsureIndexExistsAsync(
        string prefix,
        string entity,
        string version,
        IReadOnlyCollection<SearchableFieldDescriptor> fields,
        CancellationToken cancellationToken = default)
    {
        var indexName = BuildIndexName(prefix, entity, version);
        var existsResponse = await client.Indices.ExistsAsync(indexName, cancellationToken);

        if (existsResponse.Exists)
        {
            logger.LogDebug("Index {IndexName} already exists. Skipping create.", indexName);
            return indexName;
        }

        _ = await CreateIndexAsync(indexName, fields, cancellationToken);
        return indexName;
    }

    public Task<string> CreateVersionedIndexAsync(
        string prefix,
        string entity,
        string version,
        IReadOnlyCollection<SearchableFieldDescriptor> fields,
        CancellationToken cancellationToken = default)
    {
        return EnsureIndexExistsAsync(prefix, entity, version, fields, cancellationToken);
    }

    public async Task SwapAliasAsync(
        string prefix,
        string entity,
        string version,
        CancellationToken cancellationToken = default)
    {
        var aliasName = BuildAliasName(prefix, entity);
        var targetIndexName = BuildIndexName(prefix, entity, version);

        var aliasResponse = await client.Indices.GetAliasAsync(request => request.Name(aliasName), cancellationToken);
        var existingIndices = aliasResponse.IsValidResponse
            ? aliasResponse.Indices.Keys.Select(index => index.Name).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToArray()
            : [];

        var updateResponse = await client.Indices.UpdateAliasesAsync(request =>
        {
            foreach (var existingIndex in existingIndices.Where(existingIndex => !string.Equals(existingIndex, targetIndexName, StringComparison.OrdinalIgnoreCase)))
            {
                request.Actions(actions => actions.Remove(remove => remove.Index(existingIndex).Alias(aliasName)));
            }

            request.Actions(actions => actions.Add(add => add.Index(targetIndexName).Alias(aliasName)));
            return request;
        }, cancellationToken);

        if (!updateResponse.IsValidResponse)
        {
            logger.LogError("Failed to swap alias {AliasName} to index {IndexName}.", aliasName, targetIndexName);
            throw new InvalidOperationException($"Failed to swap alias '{aliasName}' to index '{targetIndexName}'.");
        }
    }

    public async Task<string> RebuildIndexAsync(
        string prefix,
        string entity,
        string version,
        IReadOnlyCollection<SearchableFieldDescriptor> fields,
        Func<string, CancellationToken, Task>? rebuildAction = null,
        CancellationToken cancellationToken = default)
    {
        var indexName = await CreateVersionedIndexAsync(prefix, entity, version, fields, cancellationToken);

        if (rebuildAction is not null)
        {
            await rebuildAction(indexName, cancellationToken);
        }

        await SwapAliasAsync(prefix, entity, version, cancellationToken);
        return indexName;
    }

    private async Task<CreateIndexResponse> CreateIndexAsync(
        string indexName,
        IReadOnlyCollection<SearchableFieldDescriptor> fields,
        CancellationToken cancellationToken)
    {
        var response = await client.Indices.CreateAsync(indexName, request => request
            .Mappings(new TypeMapping
            {
                Properties = BuildProperties(fields)
            }), cancellationToken);

        if (!response.IsValidResponse)
        {
            logger.LogError("Failed to create index {IndexName}.", indexName);
            throw new InvalidOperationException($"Failed to create index '{indexName}'.");
        }

        return response;
    }

    private static Properties BuildProperties(IReadOnlyCollection<SearchableFieldDescriptor> fields)
    {
        var properties = new Properties();

        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field.Name))
            {
                continue;
            }

            var name = field.Name.Trim();
            properties[name] = BuildProperty(field);
        }

        return properties;
    }

    private static IProperty BuildProperty(SearchableFieldDescriptor field)
    {
        return field.Type switch
        {
            SearchableFieldType.Text => BuildTextProperty(field),
            SearchableFieldType.Integer => new IntegerNumberProperty(),
            SearchableFieldType.Long => new LongNumberProperty(),
            SearchableFieldType.Double => new DoubleNumberProperty(),
            SearchableFieldType.Boolean => new BooleanProperty(),
            SearchableFieldType.Date => new DateProperty(),
            _ => new KeywordProperty()
        };
    }

    private static IProperty BuildTextProperty(SearchableFieldDescriptor field)
    {
        var textProperty = new TextProperty
        {
            Index = field.IsSearchable,
        };

        if (field.IsFilterable || field.IsSortable)
        {
            textProperty.Fields = new Properties
            {
                ["keyword"] = new KeywordProperty
                {
                    IgnoreAbove = 256,
                    Index = field.IsFilterable || field.IsSortable
                }
            };
        }

        return textProperty;
    }

    private static string BuildIndexName(string prefix, string entity, string version)
        => $"{Normalize(prefix)}-{Normalize(entity)}-{Normalize(version)}";

    private static string BuildAliasName(string prefix, string entity)
        => $"{Normalize(prefix)}-{Normalize(entity)}";

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
        }

        return value.Trim().ToLowerInvariant();
    }
}
