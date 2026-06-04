namespace Vitarish.Feed.Domain.Models;

/// <summary>
/// Represents a single item in a feed, providing a unified structure for displaying
/// various types of content (attachments, projects, events, etc.) from different sources.
/// </summary>
public record FeedItem
{
    /// <summary>
    /// Unique identifier for this feed item
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Identifier of the feed data source that provided this item
    /// </summary>
    public required string SourceId { get; init; }

    /// <summary>
    /// Display name of the source (e.g., "Sea Attachments", "Sea Projects")
    /// </summary>
    public required string SourceName { get; init; }

    /// <summary>
    /// Title or primary text of the feed item
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Optional description or body text
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Timestamp when this item was created or last updated
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Type/category of the item (e.g., "attachment", "project", "event")
    /// </summary>
    public required string ItemType { get; init; }

    /// <summary>
    /// Optional URL for the item's icon or image
    /// </summary>
    public string? IconUrl { get; init; }

    /// <summary>
    /// Optional URL to navigate to when clicking the item
    /// </summary>
    public string? NavigationUrl { get; init; }

    /// <summary>
    /// User who created or owns this item
    /// </summary>
    public string? CreatedBy { get; init; }

    /// <summary>
    /// Additional metadata for rendering customization
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();
}
