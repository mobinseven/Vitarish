using Riok.Mapperly.Abstractions;

namespace Vitarish.Commerce.Domain.Mappers;

public partial class EntityMapper
{
    public partial global::Vitarish.Commerce.Domain.Dto.OrderReadDto ToRead(global::Vitarish.Commerce.Domain.Entities.Order src);

    [MapperIgnoreSource("Stats")]
    public partial global::Vitarish.Commerce.Domain.Entities.Order ToEntity(global::Vitarish.Commerce.Domain.Dto.OrderWriteDto src);

    public partial global::Vitarish.Commerce.Domain.Dto.OrderWriteDto ToWrite(global::Vitarish.Commerce.Domain.Dto.OrderReadDto src);

    public partial global::Vitarish.Commerce.Domain.Models.OrderDescriptor ToDescriptor(global::Vitarish.Commerce.Domain.Dto.OrderDescriptorDto src);
}
