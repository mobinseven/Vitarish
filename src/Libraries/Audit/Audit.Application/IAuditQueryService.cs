using Vitarish.Audit.Domain;

namespace Vitarish.Audit.Application;

public interface IAuditQueryService
{
    Task<IReadOnlyList<AuditEntryReadDto>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEntryReadDto>> GetByActorAsync(string actorId, CancellationToken cancellationToken);
}
