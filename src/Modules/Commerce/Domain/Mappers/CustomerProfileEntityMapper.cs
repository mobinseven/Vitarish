namespace Vitarish.Commerce.Domain.Mappers;

public partial class EntityMapper
{
    public partial global::Vitarish.Commerce.Domain.Dto.CustomerProfileReadDto ToRead(global::Vitarish.Commerce.Domain.Entities.CustomerProfile src);

    public partial global::Vitarish.Commerce.Domain.Entities.CustomerProfile ToDescriptor(global::Vitarish.Commerce.Domain.Dto.CustomerProfileWriteDto src);

    public partial global::Vitarish.Commerce.Domain.Dto.CustomerProfileWriteDto ToWrite(global::Vitarish.Commerce.Domain.Dto.CustomerProfileReadDto src);

    public partial global::Vitarish.Commerce.Domain.Models.CustomerProfileDescriptor ToDescriptor(global::Vitarish.Commerce.Domain.Dto.CustomerProfileDescriptorDto src);
}
