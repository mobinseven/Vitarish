#nullable enable

namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for EventRole entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    /// <summary>
    /// Converts a EventRole entity to its read DTO representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Dto.EventRoleReadDto ToDto(global::Vitarish.Sea.Domain.Entities.EventRole src);

    /// <summary>
    /// Converts a EventRole write DTO to its entity representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Entities.EventRole ToDomain(global::Vitarish.Sea.Domain.Dto.EventRoleWriteDto src);

    /// <summary>
    /// Converts a EventRole descriptor DTO to its descriptor representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Models.EventRoleDescriptor ToDomain(global::Vitarish.Sea.Domain.Dto.EventRoleDescriptorDto src);
}
