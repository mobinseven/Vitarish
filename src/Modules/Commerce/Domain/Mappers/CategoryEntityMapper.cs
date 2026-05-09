namespace Vitarish.Commerce.Domain.Mappers;

public partial class EntityMapper
{
    public partial global::Vitarish.Commerce.Domain.Dto.CategoryReadDto ToDto(global::Vitarish.Commerce.Domain.Entities.Category src);

    public partial global::Vitarish.Commerce.Domain.Entities.Category ToDomain(global::Vitarish.Commerce.Domain.Dto.CategoryWriteDto src);

    public partial global::Vitarish.Commerce.Domain.Dto.CategoryWriteDto ToWrite(global::Vitarish.Commerce.Domain.Dto.CategoryReadDto src);
}
