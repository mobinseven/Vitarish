namespace Vitarish.Contents.Domain.Models;

[EntityDescriptorFor(typeof(Term))]
public record TermDescriptor : EntityDescriptor
{
    public TermKind? Kind { get; init; }
    public Guid? ParentId { get; init; }
    public string? Slug { get; init; }
    public string? Name { get; init; }
}
