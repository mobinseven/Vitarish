using MediatR;
using Vitarish.Feed.Application.Queries;
using Vitarish.Feed.Domain.Abstractions;
using Vitarish.Feed.Domain.Models;

namespace Vitarish.Feed.Infrastructure.Handlers;

public class GetFeedQueryHandler(IFeedService feedService) : IRequestHandler<GetFeedQuery, IReadOnlyList<FeedItem>>
{
    public async Task<IReadOnlyList<FeedItem>> Handle(
        GetFeedQuery request,
        CancellationToken cancellationToken) => await feedService.GetFeedAsync(request.Options, cancellationToken);
}
