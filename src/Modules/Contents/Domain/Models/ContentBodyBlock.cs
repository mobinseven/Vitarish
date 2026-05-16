namespace Vitarish.Contents.Domain.Models;

public record ContentBodyBlock
{
    public string Type { get; init; } = "paragraph";
    public string? Text { get; init; }
    public Guid? AssetId { get; init; }
    public Dictionary<string, object?> Metadata { get; init; } = [];
}
