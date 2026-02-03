namespace Vitarish.Cms.Domain.Models;

[EntityDescriptorFor(typeof(ContentItem))]
public record ContentDescriptor : EntityDescriptor
{
    public ContentStatus? Status { get; init; }
    public Guid? TagId { get; init; }
    public Guid? CategoryId { get; init; }
}
