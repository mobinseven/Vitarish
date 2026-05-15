using Vitarish.Requests.Application.Abstractions;
using Vitarish.Search.Domain.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Contents.Infrastructure.SearchProviders;

internal sealed class ContentItemsSearchProvider(
    ContentsDbContext dbContext,
    IRequestContext requestContext) : ISearchIndexProvider
{
    public const string IndexNameValue = "contents.items";

    public string IndexName => IndexNameValue;

    public async Task<IReadOnlyList<SearchDocument>> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        string tenantId = !string.IsNullOrWhiteSpace(requestContext.TenantId)
            ? requestContext.TenantId
            : throw new InvalidOperationException("Tenant ID must be set before indexing content documents.");

        var rows = await dbContext.ContentItems
            .AsNoTracking()
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
            Body = $"{c.Excerpt}\n{JsonSerializer.Serialize(c.Body)}",
            Type = c.Type.ToString(),
            TenantId = tenantId,
            NavigationUrl = c.CanonicalPath,
            Metadata = new Dictionary<string, object?>
            {
                ["locale"] = c.Locale,
                ["authorId"] = c.AuthorId,
                ["publishedAt"] = c.PublishedAt
            }
        })];
    }
}
