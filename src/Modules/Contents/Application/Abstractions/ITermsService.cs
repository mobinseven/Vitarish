using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Contents.Application.Abstractions;

public interface ITermsService : IEntityService<TermReadDto, TermWriteDto, TermDescriptorDto>;
