#nullable enable

namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for Receipt entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    /// <summary>
    /// Converts a Receipt entity to its read DTO representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Dto.ReceiptReadDto ToDto(global::Vitarish.Sea.Domain.Entities.Receipt src);

    /// <summary>
    /// Converts a Receipt write DTO to its entity representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Entities.Receipt ToDomain(global::Vitarish.Sea.Domain.Dto.ReceiptWriteDto src);

    /// <summary>
    /// Converts a Receipt descriptor DTO to its descriptor representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Models.ReceiptDescriptor ToDomain(global::Vitarish.Sea.Domain.Dto.ReceiptDescriptorDto src);
}
