using Vitarish.Entities.Server.Models;

namespace Vitarish.Parties.Domain.Dtos;

/// <summary>
/// DTO for writing (creating/updating) Owner entities.
/// </summary>
public record OwnerWriteDto : EntityWriteDto
{
    public string? Name { get; set; }
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
}
