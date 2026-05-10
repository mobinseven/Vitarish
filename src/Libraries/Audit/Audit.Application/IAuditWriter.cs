using Vitarish.Audit.Domain;

namespace Vitarish.Audit.Application;

public interface IAuditWriter
{
    Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken);
    Task WriteBatchAsync(IReadOnlyList<AuditEntry> entries, CancellationToken cancellationToken);
}
