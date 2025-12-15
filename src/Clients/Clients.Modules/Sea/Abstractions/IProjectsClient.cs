using Vitarish.Clients.Modules.Sea.Dto;
using Vorn.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Abstractions;

public interface IProjectsClient : IEntityClient<ProjectDto, ProjectDescriptorDto>
{
    Task<Guid> CreateProjectAsync(string name, string description, bool template, string metadata, CancellationToken cancellationToken = default);
    Task UpdateProjectDetailsAsync(Guid id, string name, string description, bool template, string metadata, CancellationToken cancellationToken = default);
    Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken = default);
    Task<ProjectSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default);
}
