#nullable enable

namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for Procedure entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    /// <summary>
    /// Converts a Procedure entity to its read DTO representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Dto.ProcedureReadDto ToDto(global::Vitarish.Sea.Domain.Entities.Procedure src);

    /// <summary>
    /// Converts a Procedure write DTO to its entity representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Entities.Procedure ToDomain(global::Vitarish.Sea.Domain.Dto.ProcedureWriteDto src);

    /// <summary>
    /// Converts a Procedure descriptor DTO to its descriptor representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Models.ProcedureDescriptor ToDomain(global::Vitarish.Sea.Domain.Dto.ProcedureDescriptorDto src);

    private static string MapMetadata(Dictionary<string, object?> metadata)
        => global::Vitarish.Sea.Domain.Entities.Procedure.SerializeMetadata(metadata);

    private static Dictionary<string, object?> MapMetadata(string metadata)
        => global::Vitarish.Sea.Domain.Entities.Procedure.DeserializeMetadata(metadata);
}
