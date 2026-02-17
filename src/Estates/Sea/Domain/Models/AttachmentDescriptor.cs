namespace Vitarish.Sea.Domain.Models;

[EntityDescriptorFor(typeof(Attachment))]
public record AttachmentDescriptor : EntityDescriptor
{
    public Guid? EventId { get; init; }
    public string? Content { get; init; }
    public string? ContentType { get; init; }
    public string? ContentThumbnail { get; init; }
}

