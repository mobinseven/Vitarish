namespace Vitarish.Contents.Domain.Models;

[EntityDescriptorFor(typeof(ContentAsset))]
public record ContentAssetDescriptor : EntityDescriptor
{
    public Guid? ContentId { get; init; }
    public Guid? AssetId { get; init; }
    public ContentAssetRole? Role { get; init; }
}
