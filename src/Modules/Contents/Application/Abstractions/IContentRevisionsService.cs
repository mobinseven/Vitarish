using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Contents.Application.Abstractions;

public interface IContentRevisionsService : IEntityService<ContentRevisionReadDto, ContentRevisionWriteDto, ContentRevisionDescriptorDto>;
