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

        dbContext.UseRowScope(entry.TenantId);
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

        // Each entry belongs to the tenant of the entity it describes, so a
        // request that writes another tenant's entities audits them there.
        foreach (IGrouping<string, AuditEntry> scope in entries.GroupBy(entry => entry.TenantId, StringComparer.Ordinal))
        {
            dbContext.ChangeTracker.Clear();
            dbContext.UseRowScope(scope.Key);
            await dbContext.AuditEntries.AddRangeAsync(scope, cancellationToken);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
