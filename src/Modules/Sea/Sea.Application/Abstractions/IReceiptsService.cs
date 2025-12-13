using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Application.Abstractions;

public interface IReceiptsService : IEntityService<ReceiptDto, ReceiptDescriptorDto>
{
    Task<Guid> AddAsync(Guid userId, Guid objectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetReadersAsync(Guid objectId, CancellationToken cancellationToken);
    Task MarkProjectReadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);
    Task MarkProjectUnreadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);
    Task MarkProcedureReadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken);
    Task MarkProcedureUnreadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken);
    Task MarkEventReadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken);
    Task MarkEventUnreadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken);
    Task<bool> GetAsync(Guid userId, Guid objectId, CancellationToken cancellationToken);
}
