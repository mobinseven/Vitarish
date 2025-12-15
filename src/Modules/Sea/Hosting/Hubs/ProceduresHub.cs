using Vorn.Entities;
using Vorn.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class ProceduresHub : EntityServer<ProcedureDto, ProcedureDescriptorDto, IProceduresService>
{
    private readonly IProceduresService _service;

    public ProceduresHub(IProceduresService service, IEntityUserService entityUserService)
        : base(service, entityUserService)
    {
        _service = service;
    }

    public Task<Guid> CreateProcedureAsync(Guid projectId, string name, CancellationToken cancellationToken) =>
        _service.AddAsync(projectId, name, cancellationToken);

    public Task UpdateProcedureNameAsync(Guid id, string name, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, name, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        _service.DeleteAsync(id, cancellationToken);

    public Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken) =>
        _service.UpdateMetadataAsync(id, metadata, cancellationToken);

    public Task<ProcedureSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken) =>
        _service.CaptureSnapshotAsync(id, cancellationToken);
}
