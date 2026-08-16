using Vitarish.Audit.Domain;

namespace Vitarish.Audit.Application;

public interface ISecurityAuditWriter
{
    /// <summary>
    /// Persists one mandatory event. Callers own retries and must reuse the
    /// idempotency key for the same initiating operation.
    /// </summary>
    Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken);
}
