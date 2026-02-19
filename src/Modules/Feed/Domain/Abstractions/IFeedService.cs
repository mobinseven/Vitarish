using Vitarish.Feed.Domain.Models;

namespace Vitarish.Feed.Domain.Abstractions;

/// <summary>
/// Service that aggregates feed items from multiple data sources.
/// Responsible for discovering sources, fetching data, and sorting/filtering results.
/// </summary>
public interface IFeedService
{
    /// <summary>
    /// Gets all registered feed data sources
    /// </summary>
    /// <returns>List of available feed sources</returns>
    IReadOnlyList<IFeedDataSource> GetAvailableSources();

    /// <summary>
    /// Fetches aggregated feed items from all enabled sources
    /// </summary>
    /// <param name="options">Query options for filtering and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aggregated and sorted feed items</returns>
    Task<IReadOnlyList<FeedItem>> GetFeedAsync(
        FeedQueryOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the feed configuration from tenant metadata
    /// </summary>
    /// <returns>Feed configuration including enabled sources and preferences</returns>
    FeedConfiguration GetConfiguration();

    /// <summary>
    /// Updates the feed configuration in tenant metadata
    /// </summary>
    /// <param name="configuration">New feed configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateConfigurationAsync(
        FeedConfiguration configuration,
        CancellationToken cancellationToken = default);
}
