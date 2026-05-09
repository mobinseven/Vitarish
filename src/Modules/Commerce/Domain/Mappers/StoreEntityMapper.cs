using Riok.Mapperly.Abstractions;

namespace Vitarish.Commerce.Domain.Mappers;

public partial class EntityMapper
{
    public partial global::Vitarish.Commerce.Domain.Dto.StoreReadDto ToDto(global::Vitarish.Commerce.Domain.Entities.Store src);

    [MapperIgnoreSource("Stats")]
    [MapperIgnoreSource("AdminApiToken")]
    [MapperIgnoreSource("StorefrontPublishableKey")]
    public partial global::Vitarish.Commerce.Domain.Entities.Store ToDomain(global::Vitarish.Commerce.Domain.Dto.StoreWriteDto src);

    public partial global::Vitarish.Commerce.Domain.Dto.StoreWriteDto ToWrite(global::Vitarish.Commerce.Domain.Dto.StoreReadDto src);

    public partial global::Vitarish.Commerce.Domain.Models.StoreDescriptor ToDomain(global::Vitarish.Commerce.Domain.Dto.StoreDescriptorDto src);
}
