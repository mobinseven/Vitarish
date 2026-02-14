#nullable enable

namespace Vitarish.Parties.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for TenantIndicator entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    /// <summary>
    /// Converts a TenantIndicator entity to its read DTO representation.
    /// </summary>
    public partial global::Vitarish.Parties.Domain.Dtos.TenantIndicatorReadDto ToDto(global::Vitarish.Parties.Domain.Entities.TenantIndicator src);

    /// <summary>
    /// Converts a TenantIndicator write DTO to its entity representation.
    /// </summary>
    public partial global::Vitarish.Parties.Domain.Entities.TenantIndicator ToDomain(global::Vitarish.Parties.Domain.Dtos.TenantIndicatorWriteDto src);

    /// <summary>
    /// Converts a TenantIndicator descriptor DTO to its descriptor representation.
    /// </summary>
    public partial global::Vitarish.Parties.Domain.Descriptors.TenantIndicatorDescriptor ToDomain(global::Vitarish.Parties.Domain.Dtos.TenantIndicatorDescriptorDto src);
}
