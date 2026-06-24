using Vitarish.Entities.Contracts;

namespace Vitarish.Parties.Domain.Dtos;

/// <summary>
/// DTO for filtering and querying Tenant entities.
/// </summary>
public record TenantDescriptorDto : EntityDescriptorDto
{
    public Guid? OwnerId { get; set; }
    public string? Name { get; set; }
    public string? AppId { get; set; }
}
