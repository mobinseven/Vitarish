using Vitarish.Feed.Contracts;

namespace Vitarish.Feed.Client;

public interface IFeedClient
{
    Task<FeedClientResult<IReadOnlyList<FeedItemResponse>>> QueryFeedItemsAsync(FeedQueryParameters parameters, CancellationToken cancellationToken);
    Task<FeedClientResult<IReadOnlyList<FeedSourceResponse>>> QueryFeedSourcesAsync(CancellationToken cancellationToken);
    Task<FeedClientResult<FeedConfigurationResponse>> QueryFeedConfigurationAsync(CancellationToken cancellationToken);
}
