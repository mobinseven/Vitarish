namespace Vitarish.Commerce.Domain.Mappers;

public partial class EntityMapper
{
    public partial global::Vitarish.Commerce.Domain.Dto.CartReadDto ToDto(global::Vitarish.Commerce.Domain.Entities.Cart src);

    public partial global::Vitarish.Commerce.Domain.Entities.Cart ToDomain(global::Vitarish.Commerce.Domain.Dto.CartWriteDto src);

    public partial global::Vitarish.Commerce.Domain.Dto.CartWriteDto ToWrite(global::Vitarish.Commerce.Domain.Dto.CartReadDto src);

    public partial global::Vitarish.Commerce.Domain.Models.CartDescriptor ToDomain(global::Vitarish.Commerce.Domain.Dto.CartDescriptorDto src);
}
