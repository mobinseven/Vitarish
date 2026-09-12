using Vitarish.Audit.Domain;

namespace Vitarish.Audit.Application;

public interface ISecurityAuditQueryService
{
    Task<SecurityAuditPageReadDto> QueryAsync(SecurityAuditQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<SecurityAuditEventReadDto>> GetByResourceAsync(string resource, CancellationToken cancellationToken);
    Task<SecurityAuditEventReadDto?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
}
