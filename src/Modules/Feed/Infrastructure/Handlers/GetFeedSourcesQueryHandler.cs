using MediatR;
using Vitarish.Feed.Application.Queries;
using Vitarish.Feed.Domain.Abstractions;

namespace Vitarish.Feed.Infrastructure.Handlers;

public class GetFeedSourcesQueryHandler(IFeedService feedService) : IRequestHandler<GetFeedSourcesQuery, IReadOnlyList<IFeedDataSource>>
{
    public Task<IReadOnlyList<IFeedDataSource>> Handle(
        GetFeedSourcesQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<IFeedDataSource> sources = feedService.GetAvailableSources();
        return Task.FromResult(sources);
    }
}
