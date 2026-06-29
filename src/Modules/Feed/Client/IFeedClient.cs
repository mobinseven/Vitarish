using Vitarish.Feed.Contracts;

namespace Vitarish.Feed.Client;

public interface IFeedClient
{
    Task<FeedClientResult<IReadOnlyList<FeedItemResponse>>> QueryFeedItemsAsync(FeedQueryParameters parameters, IReadOnlyList<string>? sourceIds, IReadOnlyList<string>? itemTypes, CancellationToken cancellationToken);
    Task<FeedClientResult<IReadOnlyList<FeedSourceResponse>>> QueryFeedSourcesAsync(CancellationToken cancellationToken);
    Task<FeedClientResult<FeedConfigurationResponse>> QueryFeedConfigurationAsync(CancellationToken cancellationToken);
}
