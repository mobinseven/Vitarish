using Vitarish.Requests.Application.Abstractions;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Sea.Infrastructure.SearchProviders;

/// <summary>
/// Exposes Sea events for full-text indexing via the Search module.
/// </summary>
internal sealed class SeaEventsSearchProvider(SeaDbContext dbContext, IRequestContext requestContext) : ISearchIndexProvider
{
    public string IndexName => "sea.events";

    public async Task<IReadOnlyList<SearchDocument>> GetDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        string tenantId = !string.IsNullOrWhiteSpace(requestContext.TenantId)
            ? requestContext.TenantId
            : throw new InvalidOperationException("Tenant ID must be set before indexing search documents.");

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
            TenantId = tenantId,
            NavigationUrl = $"/sea/events/{e.Id}",
            Metadata = new Dictionary<string, object?>
            {
                ["projectId"] = e.ProjectId,
                ["procedureId"] = e.ProcedureId
            }
        })];
    }
}
