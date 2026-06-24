using Vitarish.Entities.Contracts;

namespace Vitarish.Parties.Domain.Dtos;

/// <summary>
/// DTO for reading Owner entities.
/// </summary>
public record OwnerReadDto : EntityReadDto
{
    public string? Name { get; set; }
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
}
