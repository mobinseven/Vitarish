using MediatR;
using Vitarish.Feed.Application.Queries;
using Vitarish.Feed.Domain.Abstractions;
using Vitarish.Feed.Domain.Models;

namespace Vitarish.Feed.Infrastructure.Handlers;

public class GetFeedConfigurationQueryHandler(IFeedService feedService) : IRequestHandler<GetFeedConfigurationQuery, FeedConfiguration>
{
    public Task<FeedConfiguration> Handle(
        GetFeedConfigurationQuery request,
        CancellationToken cancellationToken)
    {
        FeedConfiguration configuration = feedService.GetConfiguration();
        return Task.FromResult(configuration);
    }
}
