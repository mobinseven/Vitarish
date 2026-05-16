using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Contents.Application.Abstractions;

public interface IContentTermsService : IEntityService<ContentTermReadDto, ContentTermWriteDto, ContentTermDescriptorDto>;
