using Microsoft.EntityFrameworkCore;
using Vitarish.Search.Domain.Abstractions;
using Vitarish.Search.Domain.Models;
using Vitarish.Sea.Infrastructure.Data;

namespace Vitarish.Sea.Infrastructure.SearchProviders;

/// <summary>
/// Exposes Sea procedures for full-text indexing via the Search module.
/// </summary>
internal sealed class SeaProceduresSearchProvider(SeaDbContext dbContext) : ISearchIndexProvider
{
    public string IndexName => "sea.procedures";

    public async Task<IReadOnlyList<SearchDocument>> GetDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Procedures
            .AsNoTracking()
            .Select(p => new { p.Id, p.Name, p.ProjectId })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(p => new SearchDocument
        {
            Id = p.Id.ToString(),
            Title = p.Name,
            Type = "procedure",
            NavigationUrl = $"/sea/procedures/{p.Id}",
            Metadata = new Dictionary<string, object?>
            {
                ["projectId"] = p.ProjectId
            }
        })];
    }
}
