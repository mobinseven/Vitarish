using Vitarish.Search.Domain.Abstractions;
using Vitarish.Search.Domain.Models;
using Vitarish.Sea.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Vitarish.Sea.Infrastructure.SearchProviders;

/// <summary>
/// Exposes Sea projects for full-text indexing via the Search module.
/// </summary>
internal sealed class SeaProjectsSearchProvider(SeaDbContext dbContext) : ISearchIndexProvider
{
    public string IndexName => "sea.projects";

    public async Task<IReadOnlyList<SearchDocument>> GetDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        List<SearchDocument> documents = await dbContext.Projects
            .AsNoTracking()
            .Select(p => new SearchDocument
            {
                Id = p.Id.ToString(),
                Title = p.Name,
                Body = p.Description,
                Type = "project",
                NavigationUrl = $"/sea/projects/{p.Id}"
            })
            .ToListAsync(cancellationToken);

        return documents;
    }
}
