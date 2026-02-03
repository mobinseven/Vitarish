namespace Vitarish.Cms.Domain.Entities;

public class ContentCategory : Entity
{
    private Guid _contentId;
    public Guid ContentId
    {
        get => _contentId;
        private set => SetField(ref _contentId, value, nameof(ContentId));
    }

    private Guid _categoryId;
    public Guid CategoryId
    {
        get => _categoryId;
        private set => SetField(ref _categoryId, value, nameof(CategoryId));
    }

    public ContentItem? Content { get; set; }
    public Category? Category { get; set; }

    public ContentCategory() { }

    public ContentCategory(Guid contentId, Guid categoryId)
    {
        Id = Guid.NewGuid();
        _contentId = contentId;
        _categoryId = categoryId;
    }
}
