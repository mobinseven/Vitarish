#nullable enable

namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for Project entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    /// <summary>
    /// Converts a Project entity to its read DTO representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Dto.ProjectReadDto ToDto(global::Vitarish.Sea.Domain.Entities.Project src);

    /// <summary>
    /// Converts a Project write DTO to its entity representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Entities.Project ToDomain(global::Vitarish.Sea.Domain.Dto.ProjectWriteDto src);

    /// <summary>
    /// Converts a Project descriptor DTO to its descriptor representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Models.ProjectDescriptor ToDomain(global::Vitarish.Sea.Domain.Dto.ProjectDescriptorDto src);
}
