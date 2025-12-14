using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clients.Modules.Sea.Abstractions;

public interface IProjectsClient
{
    Task<Guid> AddAsync(string name, string description, bool template, string metadata, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, string name, string description, bool template, string metadata, CancellationToken cancellationToken = default);
    Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken = default);
    Task<ProjectSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default);
}
