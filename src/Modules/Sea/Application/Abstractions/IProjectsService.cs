namespace Vitarish.Sea.Application.Abstractions;

using Vitarish.Entities.Server.Abstractions;
using Vitarish.Sea.Domain.Dto;

public interface IProjectsService : IEntityService<ProjectReadDto, ProjectWriteDto, ProjectDescriptorDto>;
