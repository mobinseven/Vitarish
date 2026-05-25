using Microsoft.Extensions.DependencyInjection;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;
using Vitarish.Search.Domain.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Intelligence.Infrastructure.Services.Rag;

public sealed class SearchRagSearchBridge(IServiceProvider serviceProvider) : IRagSearchBridge
{
    public async Task<IReadOnlyList<RagRetrievedDocument>> RetrieveAsync(
        RagRetrieveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return [];
        }

        ISearchService? searchService = serviceProvider.GetService<ISearchService>();
        if (searchService is null)
        {
            return [];
        }

        try
        {
            int limit = request.MaxResults > 0 ? request.MaxResults : 5;

            SearchResult result = await searchService
                .SearchAsync(indexName: null, request.Query, new SearchOptions { Limit = limit }, cancellationToken)
                .ConfigureAwait(false);

            return [..
                result.Items
                    .Take(limit)
                    .Select((item, index) => new RagRetrievedDocument
                    {
                        Id = item.Id,
                        Content = string.Join("\n", new[] { item.Title, item.Body }.Where(x => !string.IsNullOrWhiteSpace(x))),
                        Source = string.IsNullOrWhiteSpace(item.IndexName)
                            ? item.Type
                            : $"{item.IndexName}:{item.Type}",
                        Score = 1d / (index + 1)
                    })];
        }
        catch
        {
            return [];
        }
    }
}
