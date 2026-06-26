namespace Vitarish.Files.Contracts.Metadata;

public sealed record FileMetadataDto
{
    public long Size { get; init; }
    public string? Extension { get; init; }
    public FileDimensionsDto? Dimensions { get; init; }
}

public sealed record FileDimensionsDto
{
    public int Width { get; init; }
    public int Height { get; init; }
}
