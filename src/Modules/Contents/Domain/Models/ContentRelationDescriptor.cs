namespace Vitarish.Contents.Domain.Models;

[EntityDescriptorFor(typeof(ContentRelation))]
public record ContentRelationDescriptor : EntityDescriptor
{
    public Guid? FromContentId { get; init; }
    public Guid? ToContentId { get; init; }
    public ContentRelationType? RelationType { get; init; }
}
