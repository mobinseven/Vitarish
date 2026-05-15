namespace Vitarish.Contents.Domain.Entities;

public class Term : Entity
{
    public TermKind Kind { get; set; } = TermKind.Tag;
    public Guid? ParentId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object?> Metadata { get; set; } = [];
}
