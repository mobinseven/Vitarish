using Vitarish.Entities.Client.Models;

namespace Vitarish.Parties.Application.Dtos;

/// <summary>
/// DTO for reading Tenant entities.
/// </summary>
public record TenantReadDto : EntityReadDto
{
    public Guid OwnerId { get; set; }
    public required string Name { get; set; }
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public required string ClientId { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = [];
}
