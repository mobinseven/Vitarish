using MediatR;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Application.Queries;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Search.Infrastructure.Handlers;

internal sealed class SearchQueryHandler(ISearchService searchService)
    : IRequestHandler<SearchQuery, SearchResult>
{
    public Task<SearchResult> Handle(SearchQuery request, CancellationToken cancellationToken) =>
        searchService.SearchAsync(request.IndexName, request.Query, request.Options, cancellationToken);
}
