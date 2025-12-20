using Vitarish.Entities.Server.Core;
using Vitarish.Entities.Server.Services;
using Vitarish.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class ProceduresHub(IProceduresService service, IEntityUserService? entityUserService = null)
    : EntityServer<ProcedureDto, ProcedureWriteDto, ProcedureDescriptorDto, IProceduresService>(service, entityUserService)
{
    public Task<Guid> CreateProcedureAsync(Guid projectId, string name, CancellationToken cancellationToken) =>
        EntityService.AddAsync(projectId, name, cancellationToken);

    public Task UpdateProcedureNameAsync(Guid id, string name, CancellationToken cancellationToken) =>
        EntityService.UpdateAsync(id, name, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        EntityService.DeleteAsync(id, cancellationToken);

    public Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken) =>
        EntityService.UpdateMetadataAsync(id, metadata, cancellationToken);

    public Task<ProcedureSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken) =>
        EntityService.CaptureSnapshotAsync(id, cancellationToken);
}
