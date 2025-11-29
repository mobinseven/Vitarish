using Vorn.Entities.Generators;

namespace Vorn.Files.Core.Domain.Models;

[EntityDescriptorFor(typeof(FileInformation))]
public record FileInformationDescriptor : EntityDescriptor
{
    public string? Url { get; set; }
    public string? Name { get; set; }
    public string? Owner { get; set; }
    public string? Checksum { get; set; }
    public string? Tag { get; set; }
}
