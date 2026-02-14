#nullable enable

namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for Role entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    /// <summary>
    /// Converts a Role entity to its read DTO representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Dto.RoleReadDto ToDto(global::Vitarish.Sea.Domain.Entities.Role src);

    /// <summary>
    /// Converts a Role write DTO to its entity representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Entities.Role ToDomain(global::Vitarish.Sea.Domain.Dto.RoleWriteDto src);

    /// <summary>
    /// Converts a Role descriptor DTO to its descriptor representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Models.RoleDescriptor ToDomain(global::Vitarish.Sea.Domain.Dto.RoleDescriptorDto src);
}
