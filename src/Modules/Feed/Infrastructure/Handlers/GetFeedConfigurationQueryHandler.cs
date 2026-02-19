using MediatR;
using Vitarish.Modules.Feed.Application.Queries;
using Vitarish.Modules.Feed.Domain.Abstractions;
using Vitarish.Modules.Feed.Domain.Models;

namespace Vitarish.Modules.Feed.Infrastructure.Handlers;

public class GetFeedConfigurationQueryHandler : IRequestHandler<GetFeedConfigurationQuery, FeedConfiguration>
{
    private readonly IFeedService _feedService;

    public GetFeedConfigurationQueryHandler(IFeedService feedService)
    {
        _feedService = feedService;
    }

    public Task<FeedConfiguration> Handle(
        GetFeedConfigurationQuery request,
        CancellationToken cancellationToken)
    {
        var configuration = _feedService.GetConfiguration();
        return Task.FromResult(configuration);
    }
}
