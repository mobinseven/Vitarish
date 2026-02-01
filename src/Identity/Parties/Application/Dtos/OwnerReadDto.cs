using Vitarish.Entities.Server.Models;

namespace Vitarish.Parties.Application.Dtos;

/// <summary>
/// DTO for reading Owner entities.
/// </summary>
public record OwnerReadDto : EntityReadDto
{
    public required string Name { get; set; }
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
}
