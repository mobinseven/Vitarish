namespace Vitarish.Contents.Domain.Models;

[EntityDescriptorFor(typeof(ContentRevision))]
public record ContentRevisionDescriptor : EntityDescriptor
{
    public Guid? ContentId { get; init; }
    public int? Version { get; init; }
}
