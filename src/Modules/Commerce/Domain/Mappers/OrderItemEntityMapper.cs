namespace Vitarish.Commerce.Domain.Mappers;

public partial class EntityMapper
{
    public partial global::Vitarish.Commerce.Domain.Dto.OrderItemReadDto ToDto(global::Vitarish.Commerce.Domain.Entities.OrderItem src);

    public partial global::Vitarish.Commerce.Domain.Entities.OrderItem ToDomain(global::Vitarish.Commerce.Domain.Dto.OrderItemWriteDto src);

    public partial global::Vitarish.Commerce.Domain.Dto.OrderItemWriteDto ToWrite(global::Vitarish.Commerce.Domain.Dto.OrderItemReadDto src);
}
