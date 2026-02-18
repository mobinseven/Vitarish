namespace Vitarish.Sea.Application.Abstractions;

using Vitarish.Entities.Server.Abstractions;
using Vitarish.Sea.Domain.Dto;

public interface IProjectsService : IEntityService<ProjectReadDto, ProjectWriteDto, ProjectDescriptorDto>
{
    Task<Guid> AddAsync(string name, string description, bool template, string metadata, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, string name, string description, bool template, string metadata, CancellationToken cancellationToken);
    Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken);
}
