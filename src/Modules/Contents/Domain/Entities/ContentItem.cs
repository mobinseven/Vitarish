namespace Vitarish.Contents.Domain.Entities;

public class ContentItem : Entity
{
    public ContentItemType Type { get; set; } = ContentItemType.Article;
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
    public string Slug { get; set; } = string.Empty;
    public string CanonicalPath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public IReadOnlyList<ContentBodyBlock> Body { get; set; } = [];
    public Dictionary<string, object?> Metadata { get; set; } = [];
    public Guid AuthorId { get; set; }
    public string Locale { get; set; } = "en";
    public DateTimeOffset? PublishedAt { get; set; }
}
