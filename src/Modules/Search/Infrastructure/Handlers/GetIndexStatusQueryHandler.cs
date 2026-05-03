using MediatR;
using Vitarish.Search.Application.Queries;
using Vitarish.Search.Domain.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Search.Infrastructure.Handlers;

internal sealed class GetIndexStatusQueryHandler(ISearchService searchService)
    : IRequestHandler<GetIndexStatusQuery, IReadOnlyList<IndexStatusReport>>
{
    public Task<IReadOnlyList<IndexStatusReport>> Handle(
        GetIndexStatusQuery request,
        CancellationToken cancellationToken) =>
        searchService.GetIndexStatusAsync(cancellationToken);
}
