namespace Vitarish.Files.Contracts.Metadata;

public sealed record FileInformationWriteRequest
{
    public string AllowedOrigins { get; init; } = "*";
    public string ContentType { get; init; } = string.Empty;
    public string? Thumbnail { get; init; }
    public string? Checksum { get; init; }
    public string? Url { get; init; }
    public string? Owner { get; init; }
    public string? Tag { get; init; }
}
