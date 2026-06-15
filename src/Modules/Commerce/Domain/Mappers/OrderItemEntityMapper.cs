namespace Vitarish.Commerce.Domain.Mappers;

public partial class EntityMapper
{
    public partial global::Vitarish.Commerce.Domain.Dto.OrderItemReadDto ToRead(global::Vitarish.Commerce.Domain.Entities.OrderItem src);

    public partial global::Vitarish.Commerce.Domain.Entities.OrderItem ToEntity(global::Vitarish.Commerce.Domain.Dto.OrderItemWriteDto src);

    public partial global::Vitarish.Commerce.Domain.Dto.OrderItemWriteDto ToWrite(global::Vitarish.Commerce.Domain.Dto.OrderItemReadDto src);

    public partial global::Vitarish.Commerce.Domain.Models.OrderItemDescriptor ToDescriptor(global::Vitarish.Commerce.Domain.Dto.OrderItemDescriptorDto src);
}
