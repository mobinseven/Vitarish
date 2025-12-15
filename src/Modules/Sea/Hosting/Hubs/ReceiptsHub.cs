using Vorn.Entities;
using Vorn.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class ReceiptsHub : EntityServer<ReceiptDto, ReceiptDescriptorDto, IReceiptsService>
{
    private readonly IReceiptsService _service;

    public ReceiptsHub(IReceiptsService service, IEntityUserService entityUserService)
        : base(service, entityUserService)
    {
        _service = service;
    }

    public Task<Guid> CreateReceiptAsync(Guid userId, Guid objectId, CancellationToken cancellationToken) =>
        _service.AddAsync(userId, objectId, cancellationToken);

    public Task<IReadOnlyList<Guid>> GetReadersAsync(Guid objectId, CancellationToken cancellationToken) =>
        _service.GetReadersAsync(objectId, cancellationToken);

    public Task MarkProjectReadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken) =>
        _service.MarkProjectReadAsync(userId, projectId, cancellationToken);

    public Task MarkProjectUnreadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken) =>
        _service.MarkProjectUnreadAsync(userId, projectId, cancellationToken);

    public Task MarkProcedureReadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken) =>
        _service.MarkProcedureReadAsync(userId, procedureId, cancellationToken);

    public Task MarkProcedureUnreadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken) =>
        _service.MarkProcedureUnreadAsync(userId, procedureId, cancellationToken);

    public Task MarkEventReadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken) =>
        _service.MarkEventReadAsync(userId, eventId, cancellationToken);

    public Task MarkEventUnreadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken) =>
        _service.MarkEventUnreadAsync(userId, eventId, cancellationToken);

    public Task<bool> HasReceiptAsync(Guid userId, Guid objectId, CancellationToken cancellationToken) =>
        _service.HasReceiptAsync(userId, objectId, cancellationToken);
}
