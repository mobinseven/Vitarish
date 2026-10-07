namespace Vitarish.Files.Contracts.Requests;

public sealed record FileUploadRequest
{
    /// <summary>The multipart field that carries the file bytes; the other fields are named after these properties.</summary>
    public const string ContentFieldName = "content";

    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long Size { get; init; }
    public string Checksum { get; init; } = string.Empty;
    public string? Thumbnail { get; init; }
    public string? Tag { get; init; }
}
