
namespace Vitarish.Parties.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for Owner entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    /// <summary>
    /// Converts a Owner entity to its read DTO representation.
    /// </summary>
    public partial global::Vitarish.Parties.Domain.Dtos.OwnerReadDto ToDto(global::Vitarish.Parties.Domain.Entities.Owner src);

    /// <summary>
    /// Converts a Owner write DTO to its entity representation.
    /// </summary>
    public partial global::Vitarish.Parties.Domain.Entities.Owner ToDomain(global::Vitarish.Parties.Domain.Dtos.OwnerWriteDto src);
    public partial global::Vitarish.Parties.Domain.Dtos.OwnerWriteDto ToWrite(global::Vitarish.Parties.Domain.Dtos.OwnerReadDto src);

    /// <summary>
    /// Converts a Owner descriptor DTO to its descriptor representation.
    /// </summary>
    public partial global::Vitarish.Parties.Domain.Descriptors.OwnerDescriptor ToDomain(global::Vitarish.Parties.Domain.Dtos.OwnerDescriptorDto src);
}
