namespace Vitarish.Contents.Domain.Entities;

public class ContentAsset : Entity
{
    public Guid ContentId { get; set; }
    public Guid AssetId { get; set; }
    public ContentAssetRole Role { get; set; } = ContentAssetRole.Attachment;
    public int Position { get; set; }
}
