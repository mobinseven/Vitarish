using Vitarish.Entities.Server.Core;
using Vitarish.Entities.Server.Services;
using Vitarish.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class ProjectsHub(IProjectsService service, IEntityUserService? entityUserService = null)
    : EntityServer<ProjectDto, ProjectWriteDto, ProjectDescriptorDto, IProjectsService>(service, entityUserService)
{
    public Task<Guid> CreateProjectAsync(string name, string description, bool template, string metadata, CancellationToken cancellationToken) =>
        EntityService.AddAsync(name, description, template, metadata, cancellationToken);

    public Task UpdateProjectDetailsAsync(Guid id, string name, string description, bool template, string metadata, CancellationToken cancellationToken) =>
        EntityService.UpdateAsync(id, name, description, template, metadata, cancellationToken);

    public Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken) =>
        EntityService.UpdateMetadataAsync(id, metadata, cancellationToken);

    public Task<ProjectSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken) =>
        EntityService.CaptureSnapshotAsync(id, cancellationToken);
}
