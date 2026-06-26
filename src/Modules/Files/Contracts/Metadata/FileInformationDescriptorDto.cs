namespace Vitarish.Files.Contracts.Metadata;

public sealed record FileInformationDescriptorDto
{
    public Guid? Id { get; init; }
    public int? Skip { get; init; }
    public int? Take { get; init; }
    public string? Search { get; init; }
    public string? Url { get; init; }
    public string? Name { get; init; }
    public string? Owner { get; init; }
    public string? Checksum { get; init; }
    public string? Tag { get; init; }
}
