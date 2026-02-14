#nullable enable

using Riok.Mapperly.Abstractions;

namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for Attachment entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    /// <summary>
    /// Converts a Attachment entity to its read DTO representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Dto.AttachmentReadDto ToDto(global::Vitarish.Sea.Domain.Entities.Attachment src);

    /// <summary>
    /// Converts a Attachment write DTO to its entity representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Entities.Attachment ToDomain(global::Vitarish.Sea.Domain.Dto.AttachmentWriteDto src);

    /// <summary>
    /// Converts a Attachment descriptor DTO to its descriptor representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Models.AttachmentDescriptor ToDomain(global::Vitarish.Sea.Domain.Dto.AttachmentDescriptorDto src);
}
