using Vitarish.Requests.Application.Abstractions;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Sea.Infrastructure.SearchProviders;

/// <summary>
/// Exposes Sea procedures for full-text indexing via the Search module.
/// </summary>
internal sealed class SeaProceduresSearchProvider(SeaDbContext dbContext, IRequestContext requestContext) : ISearchIndexProvider
{
    public string IndexName => "sea.procedures";

    public async Task<IReadOnlyList<SearchDocument>> GetDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        string tenantId = !string.IsNullOrWhiteSpace(requestContext.TenantId)
            ? requestContext.TenantId
            : throw new InvalidOperationException("Tenant ID must be set before indexing search documents.");

        var rows = await dbContext.Procedures
            .AsNoTracking()
            .Select(p => new { p.Id, p.Name, p.ProjectId })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(p => new SearchDocument
        {
            Id = p.Id.ToString(),
            Title = p.Name,
            Type = "procedure",
            TenantId = tenantId,
            NavigationUrl = $"/sea/procedures/{p.Id}",
            Metadata = new Dictionary<string, object?>
            {
                ["projectId"] = p.ProjectId
            }
        })];
    }
}
