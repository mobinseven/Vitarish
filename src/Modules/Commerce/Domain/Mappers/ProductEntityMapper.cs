namespace Vitarish.Commerce.Domain.Mappers;

public partial class EntityMapper
{
    public partial global::Vitarish.Commerce.Domain.Dto.ProductReadDto ToRead(global::Vitarish.Commerce.Domain.Entities.Product src);

    public partial global::Vitarish.Commerce.Domain.Entities.Product ToEntity(global::Vitarish.Commerce.Domain.Dto.ProductWriteDto src);

    public partial global::Vitarish.Commerce.Domain.Dto.ProductWriteDto ToWrite(global::Vitarish.Commerce.Domain.Dto.ProductReadDto src);

    public partial global::Vitarish.Commerce.Domain.Models.ProductDescriptor ToDescriptor(global::Vitarish.Commerce.Domain.Dto.ProductDescriptorDto src);
}
