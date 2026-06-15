namespace Vitarish.Commerce.Infrastructure.Services;

[RegisterTransient]
internal class CategoryService(IMediator mediator) : EntityService<Category, CategoryDescriptor, CategoryReadDto, CategoryWriteDto, CategoryDescriptorDto>
    (
        mediator,
        category => category.ToRead(),
        category => category.ToEntity(),
        category => category.ToDescriptor()
    ),
    ICategoryService;
