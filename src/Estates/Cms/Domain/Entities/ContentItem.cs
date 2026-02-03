namespace Vitarish.Cms.Domain.Entities;

public class ContentItem : Entity
{
    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        private set => SetField(ref _title, value, nameof(Title));
    }

    private string _slug = string.Empty;
    public string Slug
    {
        get => _slug;
        private set => SetField(ref _slug, value, nameof(Slug));
    }

    private string _bodyHtml = string.Empty;
    public string BodyHtml
    {
        get => _bodyHtml;
        private set => SetField(ref _bodyHtml, value, nameof(BodyHtml));
    }

    private string? _summary;
    public string? Summary
    {
        get => _summary;
        private set => SetField(ref _summary, value, nameof(Summary));
    }

    private ContentStatus _status = ContentStatus.Draft;
    public ContentStatus Status
    {
        get => _status;
        private set => SetField(ref _status, value, nameof(Status));
    }

    private DateTimeOffset? _publishedAt;
    public DateTimeOffset? PublishedAt
    {
        get => _publishedAt;
        private set => SetField(ref _publishedAt, value, nameof(PublishedAt));
    }

    private string? _authorId;
    public string? AuthorId
    {
        get => _authorId;
        private set => SetField(ref _authorId, value, nameof(AuthorId));
    }

    public List<ContentComment> Comments { get; } = new();
    public List<ContentTag> Tags { get; } = new();
    public List<ContentCategory> Categories { get; } = new();

    public ContentItem() { }

    public ContentItem(string title, string slug, string bodyHtml, string? summary, string? authorId)
    {
        Id = Guid.NewGuid();
        _title = title;
        _slug = slug;
        _bodyHtml = bodyHtml;
        _summary = summary;
        _authorId = authorId;
        _status = ContentStatus.Draft;
    }

    public void UpdateTitle(string title) => Title = title;

    public void UpdateSlug(string slug) => Slug = slug;

    public void UpdateBodyHtml(string bodyHtml) => BodyHtml = bodyHtml;

    public void UpdateSummary(string? summary) => Summary = summary;

    public void SetAuthor(string? authorId) => AuthorId = authorId;

    public void Publish(DateTimeOffset publishedAt)
    {
        Status = ContentStatus.Published;
        PublishedAt = publishedAt;
    }

    public void Unpublish()
    {
        Status = ContentStatus.Draft;
        PublishedAt = null;
    }

    public void Archive()
    {
        Status = ContentStatus.Archived;
    }
}
