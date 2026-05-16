using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Contents.Application.Abstractions;

public interface IContentItemsService : IEntityService<ContentItemReadDto, ContentItemWriteDto, ContentItemDescriptorDto>;
