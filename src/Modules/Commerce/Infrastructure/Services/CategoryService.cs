namespace Vitarish.Commerce.Infrastructure.Services;

[RegisterTransient]
internal class CategoryService(IMediator mediator) : EntityService<Category, CategoryDescriptor, CategoryReadDto, CategoryWriteDto, CategoryDescriptorDto>
    (
        mediator,
        category => category.ToDto(),
        category => category.ToDomain(),
        category => category.ToDomain()
    ),
    ICategoryService;
