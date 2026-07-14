
using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Sea.Application.Abstractions;

public interface IProjectsService : IEntityService<ProjectReadDto, ProjectWriteDto, ProjectDescriptorDto>
{
    Task<ProjectSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken);
}
