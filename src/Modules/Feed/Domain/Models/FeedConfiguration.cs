namespace Vitarish.Modules.Feed.Domain.Models;

/// <summary>
/// Configuration for the feed system, stored in tenant metadata.
/// Controls which sources are enabled and display preferences.
/// </summary>
public record FeedConfiguration
{
    /// <summary>
    /// List of enabled feed source IDs
    /// </summary>
    public IReadOnlyList<string> EnabledSourceIds { get; init; } = [];

    /// <summary>
    /// Default number of items to fetch per page
    /// </summary>
    public int DefaultPageSize { get; init; } = 50;

    /// <summary>
    /// Maximum number of items to fetch per page
    /// </summary>
    public int MaxPageSize { get; init; } = 100;

    /// <summary>
    /// Default sort order (e.g., "timestamp_desc", "timestamp_asc")
    /// </summary>
    public string SortOrder { get; init; } = "timestamp_desc";

    /// <summary>
    /// Whether to auto-refresh the feed periodically
    /// </summary>
    public bool AutoRefresh { get; init; } = false;

    /// <summary>
    /// Auto-refresh interval in seconds (if enabled)
    /// </summary>
    public int AutoRefreshIntervalSeconds { get; init; } = 60;

    /// <summary>
    /// Additional custom settings
    /// </summary>
    public IReadOnlyDictionary<string, object?> CustomSettings { get; init; } = new Dictionary<string, object?>();
}
