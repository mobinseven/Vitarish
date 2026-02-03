namespace Vitarish.Cms.Infrastructure.Services;

[RegisterTransient]
internal sealed class CategoriesService(IMediator mediator, CategoriesMapper mapper)
    : EntityService<Category, CategoryDescriptor, CategoryDto, CategoryWriteDto, CategoryDescriptorDto>(
        mediator,
        mapper.ToDto,
        mapper.ToDomain,
        mapper.ToDomain),
      ICategoriesService
{
}
