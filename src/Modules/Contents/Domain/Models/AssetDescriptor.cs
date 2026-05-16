namespace Vitarish.Contents.Domain.Models;

[EntityDescriptorFor(typeof(Asset))]
public record AssetDescriptor : EntityDescriptor
{
    public AssetType? Type { get; init; }
    public Guid? FileId { get; init; }
    public string? Url { get; init; }
}
