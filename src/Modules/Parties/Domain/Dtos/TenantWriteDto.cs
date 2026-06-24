using Vitarish.Entities.Contracts;

namespace Vitarish.Parties.Domain.Dtos;

/// <summary>
/// DTO for writing (creating/updating) Tenant entities.
/// </summary>
public record TenantWriteDto : EntityWriteDto
{
    public Guid OwnerId { get; set; }
    public string? Name { get; set; }
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string? AppId { get; set; }
    public new Dictionary<string, object?> Metadata { get; set; } = [];
}
