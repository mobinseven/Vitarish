namespace Vitarish.Commerce.Domain.Mappers;

public partial class EntityMapper
{
    public partial global::Vitarish.Commerce.Domain.Dto.CategoryReadDto ToRead(global::Vitarish.Commerce.Domain.Entities.Category src);

    public partial global::Vitarish.Commerce.Domain.Entities.Category ToEntity(global::Vitarish.Commerce.Domain.Dto.CategoryWriteDto src);

    public partial global::Vitarish.Commerce.Domain.Dto.CategoryWriteDto ToWrite(global::Vitarish.Commerce.Domain.Dto.CategoryReadDto src);

    public partial global::Vitarish.Commerce.Domain.Models.CategoryDescriptor ToDescriptor(global::Vitarish.Commerce.Domain.Dto.CategoryDescriptorDto src);
}
