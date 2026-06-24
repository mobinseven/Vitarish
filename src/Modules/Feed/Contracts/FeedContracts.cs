namespace Vitarish.Feed.Contracts;

public sealed record FeedQueryParameters
{
    public int? Limit { get; init; }
    public int? Skip { get; init; }
    public IReadOnlyList<string>? SourceIds { get; init; }
    public IReadOnlyList<string>? ItemTypes { get; init; }
    public DateTime? After { get; init; }
    public DateTime? Before { get; init; }
}

public sealed record FeedItemResponse(
    Guid Id,
    string SourceId,
    string SourceName,
    string Title,
    string? Description,
    DateTime Timestamp,
    string ItemType,
    string? IconUrl,
    string? NavigationUrl,
    string? CreatedBy,
    IReadOnlyDictionary<string, object?> Metadata);

public sealed record FeedSourceResponse(
    string SourceId,
    string SourceName,
    string Description,
    int Priority,
    bool EnabledByDefault);

public sealed record FeedConfigurationResponse(
    IReadOnlyList<string> EnabledSourceIds,
    int DefaultPageSize,
    int MaxPageSize,
    string SortOrder,
    bool AutoRefresh,
    int AutoRefreshIntervalSeconds,
    IReadOnlyDictionary<string, object?> CustomSettings);

public sealed record FeedErrorResponse(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);
