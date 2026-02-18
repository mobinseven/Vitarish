
namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for Receipt entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Sea.Domain.Dto.ReceiptReadDto ToDto(global::Vitarish.Sea.Domain.Entities.Receipt src);

    public partial global::Vitarish.Sea.Domain.Entities.Receipt ToDomain(global::Vitarish.Sea.Domain.Dto.ReceiptWriteDto src);

    public partial global::Vitarish.Sea.Domain.Dto.ReceiptWriteDto ToWrite(global::Vitarish.Sea.Domain.Dto.ReceiptReadDto src);

    public partial global::Vitarish.Sea.Domain.Models.ReceiptDescriptor ToDomain(global::Vitarish.Sea.Domain.Dto.ReceiptDescriptorDto src);
}
