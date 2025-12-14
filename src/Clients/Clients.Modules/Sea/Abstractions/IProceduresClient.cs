using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clients.Modules.Sea.Abstractions;

public interface IProceduresClient
{
    Task<Guid> AddAsync(Guid projectId, string name, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, string name, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken = default);
    Task<ProcedureSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default);
}
