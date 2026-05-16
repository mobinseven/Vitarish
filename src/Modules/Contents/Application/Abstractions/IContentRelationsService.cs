using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Contents.Application.Abstractions;

public interface IContentRelationsService : IEntityService<ContentRelationReadDto, ContentRelationWriteDto, ContentRelationDescriptorDto>;
