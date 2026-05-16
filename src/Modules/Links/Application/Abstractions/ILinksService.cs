using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Links.Application.Abstractions;

public interface ILinksService : IEntityService<LinkReadDto, LinkWriteDto, LinkDescriptorDto>;
