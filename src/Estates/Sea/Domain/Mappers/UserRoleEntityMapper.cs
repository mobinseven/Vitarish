#nullable enable

namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for UserRole entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    /// <summary>
    /// Converts a UserRole entity to its read DTO representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Dto.UserRoleReadDto ToDto(global::Vitarish.Sea.Domain.Entities.UserRole src);

    /// <summary>
    /// Converts a UserRole write DTO to its entity representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Entities.UserRole ToDomain(global::Vitarish.Sea.Domain.Dto.UserRoleWriteDto src);

    /// <summary>
    /// Converts a UserRole descriptor DTO to its descriptor representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Models.UserRoleDescriptor ToDomain(global::Vitarish.Sea.Domain.Dto.UserRoleDescriptorDto src);
}
