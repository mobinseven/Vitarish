using Vitarish.Entities.Server.Core;
using Vitarish.Entities.Server.Services;
using Vitarish.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class ReceiptsHub(IReceiptsService service, IEntityUserService? entityUserService = null)
    : EntityServer<ReceiptDto, ReceiptWriteDto, ReceiptDescriptorDto, IReceiptsService>(service, entityUserService)
{
    public Task<Guid> CreateReceiptAsync(Guid userId, Guid objectId, CancellationToken cancellationToken) =>
        EntityService.AddAsync(userId, objectId, cancellationToken);

    public Task<IReadOnlyList<Guid>> GetReadersAsync(Guid objectId, CancellationToken cancellationToken) =>
        EntityService.GetReadersAsync(objectId, cancellationToken);

    public Task MarkProjectReadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken) =>
        EntityService.MarkProjectReadAsync(userId, projectId, cancellationToken);

    public Task MarkProjectUnreadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken) =>
        EntityService.MarkProjectUnreadAsync(userId, projectId, cancellationToken);

    public Task MarkProcedureReadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken) =>
        EntityService.MarkProcedureReadAsync(userId, procedureId, cancellationToken);

    public Task MarkProcedureUnreadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken) =>
        EntityService.MarkProcedureUnreadAsync(userId, procedureId, cancellationToken);

    public Task MarkEventReadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken) =>
        EntityService.MarkEventReadAsync(userId, eventId, cancellationToken);

    public Task MarkEventUnreadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken) =>
        EntityService.MarkEventUnreadAsync(userId, eventId, cancellationToken);

    public Task<bool> HasReceiptAsync(Guid userId, Guid objectId, CancellationToken cancellationToken) =>
        EntityService.HasReceiptAsync(userId, objectId, cancellationToken);

    public Task<IReadOnlyDictionary<Guid, bool>> HasReceiptsAsync(Guid userId, IEnumerable<Guid> objectIds, CancellationToken cancellationToken) =>
        EntityService.HasReceiptsAsync(userId, objectIds, cancellationToken);
}
