namespace Vitarish.Cms.Domain.Entities;

public class Tag : Entity
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

    private string? _description;
    public string? Description
    {
        get => _description;
        private set => SetField(ref _description, value, nameof(Description));
    }

    public List<ContentTag> ContentTags { get; } = new();

    public Tag() { }

    public Tag(string name, string slug, string? description = null)
    {
        Id = Guid.NewGuid();
        _name = name;
        _slug = slug;
        _description = description;
    }

    public void Rename(string name) => Name = name;

    public void UpdateSlug(string slug) => Slug = slug;

    public void UpdateDescription(string? description) => Description = description;
}
