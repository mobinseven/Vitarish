using Vitarish.Requests.Application.Abstractions;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Sea.Infrastructure.SearchProviders;

/// <summary>
/// Exposes Sea projects for full-text indexing via the Search module.
/// </summary>
internal sealed class SeaProjectsSearchProvider(SeaDbContext dbContext, IRequestContext requestContext) : ISearchIndexProvider
{
    public string IndexName => "sea.projects";
    public SearchExactMatchPolicy ExactMatchPolicy => new()
    {
        IsExactMatchFirst = true,
        FieldNames = ["title.keyword"],
        PriorityTier = SearchPriorityTier.High
    };

    public async Task<IReadOnlyList<SearchDocument>> GetDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        string tenantId = !string.IsNullOrWhiteSpace(requestContext.TenantId)
            ? requestContext.TenantId
            : throw new InvalidOperationException("Tenant ID must be set before indexing search documents.");

        var rows = await dbContext.Projects
            .AsNoTracking()
            .Select(p => new { p.Id, p.Name, p.Description })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(p => new SearchDocument
        {
            Id = p.Id.ToString(),
            Title = p.Name,
            Body = p.Description,
            Type = "project",
            TenantId = tenantId,
            NavigationUrl = $"/sea/projects/{p.Id}"
        })];
    }
}
