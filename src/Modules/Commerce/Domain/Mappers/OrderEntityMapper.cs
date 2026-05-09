using Riok.Mapperly.Abstractions;

namespace Vitarish.Commerce.Domain.Mappers;

public partial class EntityMapper
{
    public partial global::Vitarish.Commerce.Domain.Dto.OrderReadDto ToDto(global::Vitarish.Commerce.Domain.Entities.Order src);

    [MapperIgnoreSource("Stats")]
    public partial global::Vitarish.Commerce.Domain.Entities.Order ToDomain(global::Vitarish.Commerce.Domain.Dto.OrderWriteDto src);

    public partial global::Vitarish.Commerce.Domain.Dto.OrderWriteDto ToWrite(global::Vitarish.Commerce.Domain.Dto.OrderReadDto src);

    public partial global::Vitarish.Commerce.Domain.Models.OrderDescriptor ToDomain(global::Vitarish.Commerce.Domain.Dto.OrderDescriptorDto src);
}
