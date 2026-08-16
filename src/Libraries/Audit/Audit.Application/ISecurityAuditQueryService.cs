using Vitarish.Audit.Domain;

namespace Vitarish.Audit.Application;

public interface ISecurityAuditQueryService
{
    Task<IReadOnlyList<SecurityAuditEventReadDto>> GetByResourceAsync(string resource, CancellationToken cancellationToken);
    Task<SecurityAuditEventReadDto?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
}
