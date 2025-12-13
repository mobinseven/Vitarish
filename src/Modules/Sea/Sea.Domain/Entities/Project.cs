namespace Vitarish.Sea.Domain.Entities;

public class Project : Entity
{
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        private set => SetField(ref _name, value, nameof(Name));
    }

    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        private set => SetField(ref _description, value, nameof(Description));
    }

    private bool _template;
    public bool Template
    {
        get => _template;
        private set => SetField(ref _template, value, nameof(Template));
    }

    private string _metadata = "{}";
    public string Metadata
    {
        get => _metadata;
        private set => SetField(ref _metadata, value, nameof(Metadata));
    }

    public ProjectStats Stats { get; set; } = new();
    public Project() { }

    public Project(string name, string description, bool template = false, string metadata = "{}")
    {
        Id = Guid.NewGuid();
        _name = name;
        _description = description;
        _template = template;
        _metadata = metadata;
    }

    public void Rename(string name)
    {
        Name = name;
    }

    public void SetDescription(string description)
    {
        Description = description;
    }

    public void SetTemplate(bool template)
    {
        Template = template;
    }

    public void SetMetadata(string metadata)
    {
        Metadata = metadata;
    }
}
