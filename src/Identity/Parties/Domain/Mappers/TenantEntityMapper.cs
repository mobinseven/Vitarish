
namespace Vitarish.Parties.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for Tenant entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    /// <summary>
    /// Converts a Tenant entity to its read DTO representation.
    /// </summary>
    public partial global::Vitarish.Parties.Domain.Dtos.TenantReadDto ToDto(global::Vitarish.Parties.Domain.Entities.Tenant src);

    /// <summary>
    /// Converts a Tenant write DTO to its entity representation.
    /// </summary>
    public partial global::Vitarish.Parties.Domain.Entities.Tenant ToDomain(global::Vitarish.Parties.Domain.Dtos.TenantWriteDto src);
    public partial global::Vitarish.Parties.Domain.Dtos.TenantWriteDto ToWrite(global::Vitarish.Parties.Domain.Dtos.TenantReadDto src);

    /// <summary>
    /// Converts a Tenant descriptor DTO to its descriptor representation.
    /// </summary>
    public partial global::Vitarish.Parties.Domain.Descriptors.TenantDescriptor ToDomain(global::Vitarish.Parties.Domain.Dtos.TenantDescriptorDto src);
}
