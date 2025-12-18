using Vitarish.Entities.Server.Core;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Entities.Server.Services;
using Vitarish.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class ProjectsHub : EntityServer<ProjectDto, ProjectWriteDto, ProjectDescriptorDto, IProjectsService>
{
    private readonly IProjectsService _service;

    public ProjectsHub(IServiceScopeFactory serviceScopeFactory, IProjectsService service, IEntityUserService? entityUserService = null)
        : base(serviceScopeFactory, entityUserService)
    {
        _service = service;
    }

    public Task<Guid> CreateProjectAsync(string name, string description, bool template, string metadata, CancellationToken cancellationToken) =>
        _service.AddAsync(name, description, template, metadata, cancellationToken);

    public Task UpdateProjectDetailsAsync(Guid id, string name, string description, bool template, string metadata, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, name, description, template, metadata, cancellationToken);

    public Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken) =>
        _service.UpdateMetadataAsync(id, metadata, cancellationToken);

    public Task<ProjectSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken) =>
        _service.CaptureSnapshotAsync(id, cancellationToken);
}
