using Vitarish.Entities.Server.Models;

namespace Vitarish.Parties.Domain.Dtos;

/// <summary>
/// DTO for reading Tenant entities.
/// </summary>
public record TenantReadDto : EntityReadDto
{
    public Guid OwnerId { get; set; }
    public required string Name { get; set; }
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public required string EstateId { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = [];
}
