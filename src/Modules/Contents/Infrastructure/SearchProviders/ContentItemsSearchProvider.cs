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

    public async Task<IReadOnlyList<SearchDocument>> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        string tenantId = !string.IsNullOrWhiteSpace(requestContext.TenantId)
            ? requestContext.TenantId
            : throw new InvalidOperationException("Tenant ID must be set before indexing content documents.");

        var rows = await dbContext.ContentItems
            .AsNoTracking()
            .Where(c => c.Status == ContentStatus.Published)
            .Select(c => new
            {
                c.Id,
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

        return [.. rows.Select(c => new SearchDocument
        {
            Id = c.Id.ToString(),
            Title = c.Title,
            Body = BuildSearchBody(c.Excerpt, c.Body),
            Type = c.Type.ToString(),
            TenantId = tenantId,
            NavigationUrl = c.CanonicalPath,
            Metadata = CreateMetadata(c.Id, c.Locale, c.AuthorId, c.PublishedAt)
        })];
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
}
