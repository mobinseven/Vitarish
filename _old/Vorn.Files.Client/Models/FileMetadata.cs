namespace Vorn.Files.Client.Models;

public record FileMetadata
{
    public long Size { get; set; }
    public string? Extension { get; set; }
    public FileDimensions? Dimensions { get; set; }
}

public record FileDimensions
{
    public int Width { get; set; }
    public int Height { get; set; }
}
