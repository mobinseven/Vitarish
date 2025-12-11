namespace Vitarish.Instagram.Scraper;

public sealed class InstagramDataDescriptor
{
    public IReadOnlyCollection<string> DirectUrls { get; init; } = Array.Empty<string>();

    public InstagramDataType DataType { get; init; } = InstagramDataType.Posts;

    public int? MaxResultsPerUrl { get; init; }

    public DateTimeOffset? FromPublishedAt { get; init; }

    public DateTimeOffset? ToPublishedAt { get; init; }

    public string? SearchQuery { get; init; }

    public IDictionary<string, object?> ExtraOptions { get; init; } = new Dictionary<string, object?>();
}
