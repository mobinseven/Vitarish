using Vitarish.Entities.Server.Models;

namespace Vitarish.Parties.Domain.Dtos;

/// <summary>
/// DTO for filtering and querying Owner entities.
/// </summary>
public record OwnerDescriptorDto : EntityDescriptorDto
{
    public string? Name { get; set; }
}
