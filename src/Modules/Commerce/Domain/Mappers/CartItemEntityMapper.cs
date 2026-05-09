namespace Vitarish.Commerce.Domain.Mappers;

public partial class EntityMapper
{
    public partial global::Vitarish.Commerce.Domain.Dto.CartItemReadDto ToDto(global::Vitarish.Commerce.Domain.Entities.CartItem src);

    public partial global::Vitarish.Commerce.Domain.Entities.CartItem ToDomain(global::Vitarish.Commerce.Domain.Dto.CartItemWriteDto src);

    public partial global::Vitarish.Commerce.Domain.Dto.CartItemWriteDto ToWrite(global::Vitarish.Commerce.Domain.Dto.CartItemReadDto src);
}
