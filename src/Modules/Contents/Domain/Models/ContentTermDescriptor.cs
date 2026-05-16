namespace Vitarish.Contents.Domain.Models;

[EntityDescriptorFor(typeof(ContentTerm))]
public record ContentTermDescriptor : EntityDescriptor
{
    public Guid? ContentId { get; init; }
    public Guid? TermId { get; init; }
    public ContentTermRole? Role { get; init; }
}
