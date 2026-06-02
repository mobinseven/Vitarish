using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;

namespace Vitarish.Audit.Server;

internal sealed class AuditWriter(AuditDbContext dbContext) : IAuditWriter
{
    public async Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        if (entry.Id == Guid.Empty)
        {
            entry.Id = Guid.NewGuid();
        }

        _ = await dbContext.AuditEntries.AddAsync(entry, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task WriteBatchAsync(IReadOnlyList<AuditEntry> entries, CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
        {
            return;
        }

        foreach (AuditEntry entry in entries)
        {
            if (entry.Id == Guid.Empty)
            {
                entry.Id = Guid.NewGuid();
            }
        }

        await dbContext.AuditEntries.AddRangeAsync(entries, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
