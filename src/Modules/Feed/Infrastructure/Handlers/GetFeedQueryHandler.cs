using MediatR;
using Vitarish.Modules.Feed.Application.Queries;
using Vitarish.Modules.Feed.Domain.Abstractions;
using Vitarish.Modules.Feed.Domain.Models;

namespace Vitarish.Modules.Feed.Infrastructure.Handlers;

public class GetFeedQueryHandler : IRequestHandler<GetFeedQuery, IReadOnlyList<FeedItem>>
{
    private readonly IFeedService _feedService;

    public GetFeedQueryHandler(IFeedService feedService)
    {
        _feedService = feedService;
    }

    public async Task<IReadOnlyList<FeedItem>> Handle(
        GetFeedQuery request,
        CancellationToken cancellationToken)
    {
        return await _feedService.GetFeedAsync(request.Options, cancellationToken);
    }
}
