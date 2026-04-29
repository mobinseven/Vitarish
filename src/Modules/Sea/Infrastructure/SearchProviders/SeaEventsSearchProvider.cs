using Microsoft.EntityFrameworkCore;
using Vitarish.Search.Domain.Abstractions;
using Vitarish.Search.Domain.Models;
using Vitarish.Sea.Infrastructure.Data;

namespace Vitarish.Sea.Infrastructure.SearchProviders;

/// <summary>
/// Exposes Sea events for full-text indexing via the Search module.
/// </summary>
internal sealed class SeaEventsSearchProvider(SeaDbContext dbContext) : ISearchIndexProvider
{
    public string IndexName => "sea.events";

    public async Task<IReadOnlyList<SearchDocument>> GetDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Events
            .AsNoTracking()
            .Select(e => new { e.Id, e.Title, e.Description, e.ProjectId, e.ProcedureId })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(e => new SearchDocument
        {
            Id = e.Id.ToString(),
            Title = e.Title,
            Body = e.Description,
            Type = "event",
            NavigationUrl = $"/sea/events/{e.Id}",
            Metadata = new Dictionary<string, object?>
            {
                ["projectId"] = e.ProjectId,
                ["procedureId"] = e.ProcedureId
            }
        })];
    }
}
