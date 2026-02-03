namespace Vitarish.Cms.Domain.Models;

[EntityDescriptorFor(typeof(ContentComment))]
public record ContentCommentDescriptor : EntityDescriptor
{
    public Guid? ContentId { get; init; }
    public bool? IsApproved { get; init; }
}
