namespace Vitarish.Files.Contracts.Metadata;

public sealed record FileInformationDto
{
    public Guid Id { get; init; }
    public string AllowedOrigins { get; init; } = "*";
    public string ContentType { get; init; } = string.Empty;
    public string? Thumbnail { get; init; }
    public string? Checksum { get; init; }
    public string? Url { get; init; }
    public string? Owner { get; init; }
    public string? Tag { get; init; }
    public FileMetadataDto Metadata { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}
