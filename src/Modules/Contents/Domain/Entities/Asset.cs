namespace Vitarish.Contents.Domain.Entities;

public class Asset : Entity
{
    public AssetType Type { get; set; } = AssetType.Image;
    public Guid FileId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = [];
}
