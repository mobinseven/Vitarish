using Vitarish.Requests.Application.Abstractions;
using Vitarish.Search.Domain.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Sea.Infrastructure.SearchProviders;

/// <summary>
/// Exposes textual Sea attachments for full-text indexing via the Search module.
/// Only attachments with a text-based content type (e.g. text/plain, text/csv) are indexed;
/// binary attachments are skipped. The attachment's Content column is indexed directly.
/// </summary>
internal sealed class SeaAttachmentsSearchProvider(SeaDbContext dbContext, IRequestContext requestContext) : ISearchIndexProvider
{
    public string IndexName => "sea.attachments";

    public async Task<IReadOnlyList<SearchDocument>> GetDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        string tenantId = !string.IsNullOrWhiteSpace(requestContext.TenantId)
            ? requestContext.TenantId
            : throw new InvalidOperationException("Tenant ID must be set before indexing search documents.");

        var rows = await dbContext.Attachments
            .AsNoTracking()
            .Where(a => a.ContentType.StartsWith("text/"))
            .Select(a => new { a.Id, a.EventId, a.Content, a.ContentType })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(a => new SearchDocument
        {
            Id = a.Id.ToString(),
            Title = a.ContentType,
            Body = a.Content,
            Type = "attachment",
            TenantId = tenantId,
            NavigationUrl = $"/sea/attachments/{a.Id}",
            Metadata = new Dictionary<string, object?>
            {
                ["eventId"] = a.EventId
            }
        })];
    }
}
