
using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Sea.Application.Abstractions;

public interface IProjectsService : IEntityService<ProjectReadDto, ProjectWriteDto, ProjectDescriptorDto>;
