namespace Vitarish.Commerce.Domain.Mappers;

public partial class EntityMapper
{
    public partial global::Vitarish.Commerce.Domain.Dto.ProductReadDto ToDto(global::Vitarish.Commerce.Domain.Entities.Product src);

    public partial global::Vitarish.Commerce.Domain.Entities.Product ToDomain(global::Vitarish.Commerce.Domain.Dto.ProductWriteDto src);

    public partial global::Vitarish.Commerce.Domain.Dto.ProductWriteDto ToWrite(global::Vitarish.Commerce.Domain.Dto.ProductReadDto src);
}
