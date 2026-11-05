using System.Collections.Immutable;
using System.Text.Json;
using Vitarish.Contents.Domain.Policies;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Contents.Infrastructure.SearchProviders;

internal sealed class ContentItemsSearchProvider(
    ContentsDbContext dbContext,
    IRequestContext requestContext) : ISearchIndexProvider
{
    public const string IndexNameValue = "contents.items";
    public const string ContentIdMetadataKey = "contentId";

    public string IndexName => IndexNameValue;

    internal static SearchDocument CreateDocument(Guid contentId, ContentItemWriteDto dto, string tenantId) => new()
    {
        Id = contentId.ToString(),
        Title = dto.Title,
        Body = BuildSearchBody(dto.Excerpt, dto.Body),
        Type = dto.Type.ToString(),
        TenantId = tenantId,
        NavigationUrl = dto.CanonicalPath,
        Metadata = CreateMetadata(contentId, dto.Locale, dto.AuthorId, dto.PublishedAt)
    };

    internal static SearchDocument CreateStructuredDocument(
        Guid contentId,
        string typeKey,
        string title,
        string? excerpt,
        string canonicalPath,
        string tenantId,
        ImmutableArray<ContentFieldDefinition> fields,
        JsonElement values) => new()
        {
            Id = contentId.ToString(),
            Title = title,
            Body = BuildStructuredSearchBody(excerpt, fields, values),
            Type = typeKey,
            TenantId = tenantId,
            NavigationUrl = canonicalPath,
            Metadata = new Dictionary<string, object?>
            {
                [ContentIdMetadataKey] = contentId.ToString()
            }
        };

    public async Task<IReadOnlyList<SearchDocument>> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        string tenantId = !string.IsNullOrWhiteSpace(requestContext.TenantId)
            ? requestContext.TenantId
            : throw new InvalidOperationException("Tenant ID must be set before indexing content documents.");

        var rows = await dbContext.ContentItems
            .AsNoTracking()
            .Where(c =>
                EF.Property<string>(c, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId &&
                c.SiteId == null)
            .Select(c => new
            {
                c.Id,
                c.Status,
                c.Type,
                c.Title,
                c.Excerpt,
                c.CanonicalPath,
                c.Locale,
                c.AuthorId,
                c.PublishedAt,
                c.Body
            })
            .ToListAsync(cancellationToken);

        List<SearchDocument> documents = [.. rows
            .Where(c => ContentSearchVisibility.IsSearchable(c.Status))
            .Select(c => new SearchDocument
        {
            Id = c.Id.ToString(),
            Title = c.Title,
            Body = BuildSearchBody(c.Excerpt, c.Body),
            Type = c.Type.ToString(),
            TenantId = tenantId,
            NavigationUrl = c.CanonicalPath,
            Metadata = CreateMetadata(c.Id, c.Locale, c.AuthorId, c.PublishedAt)
        })];

        IReadOnlyList<ContentStatus> structuredStatuses = await dbContext.ContentItems.AsNoTracking()
            .Where(item =>
                EF.Property<string>(item, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId &&
                item.SiteId != null)
            .Select(item => item.Status)
            .ToListAsync(cancellationToken);
        int publishedStructuredCount = structuredStatuses.Count(ContentSearchVisibility.IsSearchable);

        var structuredRows = await (
            from item in dbContext.ContentItems.AsNoTracking()
            join revision in dbContext.ContentRevisions.AsNoTracking()
                on item.PublishedRevisionId equals (Guid?)revision.Id
            join type in dbContext.ContentTypes.AsNoTracking()
                on item.ContentTypeId equals (Guid?)type.Id
            join schema in dbContext.ContentTypeSchemas.AsNoTracking()
                on new { ContentTypeId = type.Id, SchemaVersion = revision.SchemaVersion }
                equals new { schema.ContentTypeId, SchemaVersion = (int?)schema.Version }
            where EF.Property<string>(item, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId &&
                  EF.Property<string>(revision, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId &&
                  EF.Property<string>(type, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId &&
                  EF.Property<string>(schema, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId &&
                  item.SiteId != null && item.Status == ContentStatus.Published &&
                  item.PublishedRevisionId != null && revision.ContentId == item.Id
            select new
            {
                item.Id,
                item.Status,
                revision.Title,
                revision.Excerpt,
                revision.CanonicalPath,
                RevisionFields = revision.Fields,
                SchemaFields = schema.Fields,
                TypeKey = type.Key
            }).ToListAsync(cancellationToken);

        if (structuredRows.Count != publishedStructuredCount)
        {
            throw new InvalidOperationException("A published structured entry has an incomplete revision or schema mapping.");
        }

        foreach (var content in structuredRows)
        {
            if (!ContentSearchVisibility.IsSearchable(content.Status))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(content.Title) ||
                string.IsNullOrWhiteSpace(content.CanonicalPath) ||
                content.RevisionFields is not JsonElement fields)
            {
                throw new InvalidOperationException("A published structured entry has an incomplete revision snapshot.");
            }

            documents.Add(CreateStructuredDocument(
                content.Id,
                content.TypeKey,
                content.Title,
                content.Excerpt,
                content.CanonicalPath,
                tenantId,
                content.SchemaFields,
                fields));
        }

        return documents;
    }

    internal async Task<SearchDocument?> GetPublishedDocumentAsync(
        Guid contentId,
        string tenantId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || !string.Equals(requestContext.TenantId, tenantId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("The resolved tenant does not match the search request tenant.");
        }

        var itemState = await dbContext.ContentItems.AsNoTracking()
            .Where(item => EF.Property<string>(item, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId && item.Id == contentId)
            .Select(item => new { item.SiteId, item.Status, item.PublishedRevisionId })
            .SingleOrDefaultAsync(cancellationToken);
        if (itemState is null || itemState.SiteId is null || !ContentSearchVisibility.IsSearchable(itemState.Status))
        {
            return null;
        }

        if (itemState.PublishedRevisionId is null)
        {
            throw new InvalidOperationException("A published structured entry has no published revision pointer.");
        }

        var content = await (
            from item in dbContext.ContentItems.AsNoTracking()
            join revision in dbContext.ContentRevisions.AsNoTracking()
                on item.PublishedRevisionId equals (Guid?)revision.Id
            join type in dbContext.ContentTypes.AsNoTracking()
                on item.ContentTypeId equals (Guid?)type.Id
            join schema in dbContext.ContentTypeSchemas.AsNoTracking()
                on new { ContentTypeId = type.Id, SchemaVersion = revision.SchemaVersion }
                equals new { schema.ContentTypeId, SchemaVersion = (int?)schema.Version }
            where EF.Property<string>(item, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId &&
                  EF.Property<string>(revision, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId &&
                  EF.Property<string>(type, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId &&
                  EF.Property<string>(schema, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId &&
                  item.Id == contentId && item.SiteId != null &&
                  item.Status == ContentStatus.Published && item.PublishedRevisionId == itemState.PublishedRevisionId &&
                  revision.ContentId == item.Id
            select new
            {
                item.Id,
                item.Status,
                TypeKey = type.Key,
                revision.Title,
                revision.Excerpt,
                revision.CanonicalPath,
                RevisionFields = revision.Fields,
                SchemaFields = schema.Fields
            }).SingleOrDefaultAsync(cancellationToken);

        if (content is null)
        {
            var latestState = await dbContext.ContentItems.AsNoTracking()
                .Where(item => EF.Property<string>(item, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId && item.Id == contentId)
                .Select(item => new { item.SiteId, item.Status })
                .SingleOrDefaultAsync(cancellationToken);
            if (latestState is null || latestState.SiteId is null || !ContentSearchVisibility.IsSearchable(latestState.Status))
            {
                return null;
            }

            throw new InvalidOperationException("A published structured entry has an incomplete revision or schema mapping.");
        }

        if (!ContentSearchVisibility.IsSearchable(content.Status))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(content.Title) ||
            string.IsNullOrWhiteSpace(content.CanonicalPath) ||
            content.RevisionFields is not JsonElement fields)
        {
            throw new InvalidOperationException("A published structured entry has an incomplete revision snapshot.");
        }

        return CreateStructuredDocument(
            content.Id,
            content.TypeKey,
            content.Title,
            content.Excerpt,
            content.CanonicalPath,
            tenantId,
            content.SchemaFields,
            fields);
    }

    private static Dictionary<string, object?> CreateMetadata(Guid contentId, string locale, Guid authorId, DateTime? publishedAt) => new()
    {
        [ContentIdMetadataKey] = contentId.ToString(),
        ["locale"] = locale,
        ["authorId"] = authorId,
        ["publishedAt"] = publishedAt
    };

    private static string BuildSearchBody(string? excerpt, IEnumerable<ContentBodyBlock> blocks)
    {
        IEnumerable<string> blockText = blocks.Select(block => block.Text).OfType<string>();
        return $"{excerpt}\n{string.Join(" ", blockText)}";
    }

    private static string BuildStructuredSearchBody(
        string? excerpt,
        ImmutableArray<ContentFieldDefinition> definitions,
        JsonElement values)
    {
        List<string> textValues = [];
        if (values.ValueKind == JsonValueKind.Object)
        {
            foreach (ContentFieldDefinition definition in definitions)
            {
                if (values.TryGetProperty(definition.Key, out JsonElement value))
                {
                    AppendFieldText(definition, value, textValues);
                }
            }
        }

        IEnumerable<string> parts = string.IsNullOrWhiteSpace(excerpt)
            ? textValues
            : [excerpt, .. textValues];
        return string.Join(" ", parts);
    }

    private static void AppendFieldText(
        ContentFieldDefinition definition,
        JsonElement value,
        List<string> textValues)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return;
        }

        if (definition.Localized)
        {
            if (value.ValueKind == JsonValueKind.Object)
            {
                ContentFieldDefinition unlocalized = definition with { Localized = false };
                foreach (JsonProperty localeValue in value.EnumerateObject())
                {
                    AppendFieldText(unlocalized, localeValue.Value, textValues);
                }
            }

            return;
        }

        switch (definition.Kind)
        {
            case ContentFieldKind.Text:
            case ContentFieldKind.LongText:
                if (value.ValueKind == JsonValueKind.String && value.GetString() is string text)
                {
                    textValues.Add(text);
                }

                break;
            case ContentFieldKind.RichText:
                AppendRichText(value, textValues);
                break;
            case ContentFieldKind.List when definition.Item is not null && value.ValueKind == JsonValueKind.Array:
                foreach (JsonElement item in value.EnumerateArray())
                {
                    AppendFieldText(definition.Item, item, textValues);
                }

                break;
            case ContentFieldKind.Group when value.ValueKind == JsonValueKind.Object:
                if (!definition.Fields.IsDefaultOrEmpty)
                {
                    foreach (ContentFieldDefinition child in definition.Fields)
                    {
                        if (value.TryGetProperty(child.Key, out JsonElement childValue))
                        {
                            AppendFieldText(child, childValue, textValues);
                        }
                    }
                }

                break;
        }
    }

    private static void AppendRichText(JsonElement node, List<string> textValues)
    {
        if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement child in node.EnumerateArray())
            {
                AppendRichText(child, textValues);
            }

            return;
        }

        if (node.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (JsonProperty property in node.EnumerateObject())
        {
            if (property.Name == RichTextDocumentRules.TextProperty && property.Value.ValueKind == JsonValueKind.String)
            {
                if (property.Value.GetString() is string text)
                {
                    textValues.Add(text);
                }
            }
            else
            {
                AppendRichText(property.Value, textValues);
            }
        }
    }
}
