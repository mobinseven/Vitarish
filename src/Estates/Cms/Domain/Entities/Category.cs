namespace Vitarish.Cms.Domain.Entities;

public class Category : Entity
{
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        private set => SetField(ref _name, value, nameof(Name));
    }

    private string _slug = string.Empty;
    public string Slug
    {
        get => _slug;
        private set => SetField(ref _slug, value, nameof(Slug));
    }

    private Guid? _parentCategoryId;
    public Guid? ParentCategoryId
    {
        get => _parentCategoryId;
        private set => SetField(ref _parentCategoryId, value, nameof(ParentCategoryId));
    }

    public Category? ParentCategory { get; set; }
    public List<Category> Subcategories { get; } = new();
    public List<ContentCategory> ContentCategories { get; } = new();

    public Category() { }

    public Category(string name, string slug, Guid? parentCategoryId = null)
    {
        Id = Guid.NewGuid();
        _name = name;
        _slug = slug;
        _parentCategoryId = parentCategoryId;
    }

    public void Rename(string name) => Name = name;

    public void UpdateSlug(string slug) => Slug = slug;

    public void Move(Guid? parentCategoryId) => ParentCategoryId = parentCategoryId;
}
