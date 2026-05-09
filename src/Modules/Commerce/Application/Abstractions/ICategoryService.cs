using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Commerce.Application.Abstractions;

public interface ICategoryService : IEntityService<CategoryReadDto, CategoryWriteDto, CategoryDescriptorDto>;
