using MediatR;
using Vitarish.Modules.Feed.Application.Queries;
using Vitarish.Modules.Feed.Domain.Abstractions;

namespace Vitarish.Modules.Feed.Infrastructure.Handlers;

public class GetFeedSourcesQueryHandler : IRequestHandler<GetFeedSourcesQuery, IReadOnlyList<IFeedDataSource>>
{
    private readonly IFeedService _feedService;

    public GetFeedSourcesQueryHandler(IFeedService feedService)
    {
        _feedService = feedService;
    }

    public Task<IReadOnlyList<IFeedDataSource>> Handle(
        GetFeedSourcesQuery request,
        CancellationToken cancellationToken)
    {
        var sources = _feedService.GetAvailableSources();
        return Task.FromResult(sources);
    }
}
