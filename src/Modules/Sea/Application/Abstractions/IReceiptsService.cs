using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Sea.Application.Abstractions;

public interface IReceiptsService : IEntityService<ReceiptReadDto, ReceiptWriteDto, ReceiptDescriptorDto>
{
    Task<Guid> AddAsync(Guid userId, Guid objectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetReadersAsync(Guid objectId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, bool>> HasReceiptsAsync(Guid userId, IEnumerable<Guid> objectIds, CancellationToken cancellationToken);
    Task MarkProjectReadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);
    Task MarkProjectUnreadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);
    Task MarkProcedureReadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken);
    Task MarkProcedureUnreadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken);
    Task MarkEventReadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken);
    Task MarkEventUnreadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken);
    Task<bool> HasReceiptAsync(Guid userId, Guid objectId, CancellationToken cancellationToken);
}
