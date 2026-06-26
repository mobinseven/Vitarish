namespace Vitarish.Files.Contracts.Requests;

public sealed record FileUploadRequest
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long Size { get; init; }
    public string Checksum { get; init; } = string.Empty;
    public string? Thumbnail { get; init; }
    public string? Tag { get; init; }
}
