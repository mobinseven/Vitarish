using Vitarish.Clients.Modules.Sea.Dto;
using Vorn.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Abstractions;

public interface IReceiptsClient : IEntityClient<ReceiptDto, ReceiptDescriptorDto>
{
    Task<Guid> AddAsync(Guid userId, Guid objectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetReadersAsync(Guid objectId, CancellationToken cancellationToken = default);
    Task MarkProjectReadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default);
    Task MarkProjectUnreadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default);
    Task MarkProcedureReadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken = default);
    Task MarkProcedureUnreadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken = default);
    Task MarkEventReadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default);
    Task MarkEventUnreadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> GetAsync(Guid userId, Guid objectId, CancellationToken cancellationToken = default);
}
