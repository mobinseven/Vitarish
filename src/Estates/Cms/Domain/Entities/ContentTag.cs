namespace Vitarish.Cms.Domain.Entities;

public class ContentTag : Entity
{
    private Guid _contentId;
    public Guid ContentId
    {
        get => _contentId;
        private set => SetField(ref _contentId, value, nameof(ContentId));
    }

    private Guid _tagId;
    public Guid TagId
    {
        get => _tagId;
        private set => SetField(ref _tagId, value, nameof(TagId));
    }

    public ContentItem? Content { get; set; }
    public Tag? Tag { get; set; }

    public ContentTag() { }

    public ContentTag(Guid contentId, Guid tagId)
    {
        Id = Guid.NewGuid();
        _contentId = contentId;
        _tagId = tagId;
    }
}
