using Vitarish.Modules.Feed.Domain.Models;

namespace Vitarish.Modules.Feed.Domain.Abstractions;

/// <summary>
/// Interface that estates/modules must implement to provide feed items.
/// Each implementation represents a specific data source (e.g., Sea Attachments, Sea Projects).
/// Feed sources are auto-discovered via dependency injection.
/// </summary>
public interface IFeedDataSource
{
    /// <summary>
    /// Unique identifier for this feed source (e.g., "sea.attachments", "sea.projects")
    /// </summary>
    string SourceId { get; }

    /// <summary>
    /// Display name for this feed source (e.g., "Sea Attachments", "Recent Projects")
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// Description of what this feed source provides
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Priority/order for displaying this source (lower = higher priority)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Whether this source is enabled by default
    /// </summary>
    bool EnabledByDefault { get; }

    /// <summary>
    /// Fetches feed items from this data source
    /// </summary>
    /// <param name="options">Query options for filtering and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of feed items from this source</returns>
    Task<IReadOnlyList<FeedItem>> GetFeedItemsAsync(
        FeedQueryOptions options,
        CancellationToken cancellationToken = default);
}
