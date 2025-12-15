using Vitarish.Clients.Modules.Sea.Dto;
using Vorn.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Abstractions;

public interface IProceduresClient : IEntityClient<ProcedureDto, ProcedureDescriptorDto>
{
    Task<Guid> CreateProcedureAsync(Guid projectId, string name, CancellationToken cancellationToken = default);
    Task UpdateProcedureNameAsync(Guid id, string name, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken = default);
    Task<ProcedureSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default);
}
