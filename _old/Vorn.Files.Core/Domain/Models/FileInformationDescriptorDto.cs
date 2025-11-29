using Vorn.Entities.Models;

namespace Vorn.Files.Core.Domain.Models;

public sealed record FileInformationDescriptorDto : EntityDescriptorDto
{
    public string? Url { get; set; }
    public string? Name { get; set; }
    public string? Owner { get; set; }
    public string? Checksum { get; set; }
    public string? Tag { get; set; }
}
