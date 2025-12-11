namespace Vitarish.Instagram.Scraper;

public sealed class InstagramData
{
    public required string Id { get; init; }

    public InstagramDataType DataType { get; init; }

    public required string Url { get; init; }

    public string? AuthorUsername { get; init; }

    public string? Caption { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    public int? LikeCount { get; init; }

    public int? CommentCount { get; init; }

    public int? ViewCount { get; init; }

    public object? Raw { get; init; }
}
