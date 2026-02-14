#nullable enable

namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for Event entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    /// <summary>
    /// Converts a Event entity to its read DTO representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Dto.EventReadDto ToDto(global::Vitarish.Sea.Domain.Entities.Event src);

    /// <summary>
    /// Converts a Event write DTO to its entity representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Entities.Event ToDomain(global::Vitarish.Sea.Domain.Dto.EventWriteDto src);

    /// <summary>
    /// Converts a Event descriptor DTO to its descriptor representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Models.EventDescriptor ToDomain(global::Vitarish.Sea.Domain.Dto.EventDescriptorDto src);
}
