namespace Vitarish.Commerce.Domain.Mappers;

public partial class EntityMapper
{
    public partial global::Vitarish.Commerce.Domain.Dto.CartItemReadDto ToRead(global::Vitarish.Commerce.Domain.Entities.CartItem src);

    public partial global::Vitarish.Commerce.Domain.Entities.CartItem ToDescriptor(global::Vitarish.Commerce.Domain.Dto.CartItemWriteDto src);

    public partial global::Vitarish.Commerce.Domain.Dto.CartItemWriteDto ToWrite(global::Vitarish.Commerce.Domain.Dto.CartItemReadDto src);

    public partial global::Vitarish.Commerce.Domain.Models.CartItemDescriptor ToDescriptor(global::Vitarish.Commerce.Domain.Dto.CartItemDescriptorDto src);
}
