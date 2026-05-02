namespace Vitarish.Sea.Domain.Entities;

public class Project : Entity
{
    public string Name
    {
        get;
        set => SetField(ref field, value, nameof(Name));
    } = string.Empty;
    public string Description
    {
        get;
        set => SetField(ref field, value, nameof(Description));
    } = string.Empty;
    public bool Template
    {
        get;
        set => SetField(ref field, value, nameof(Template));
    }
    public Dictionary<string, object?> Metadata
    {
        get;
        set => SetField(ref field, value, nameof(Metadata));
    } = [];

    public ProjectStats Stats { get; set; } = new();

    public IReadOnlyList<string> UserIds { get; set; } = [];
}
