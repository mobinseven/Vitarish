using Microsoft.EntityFrameworkCore;
using Vitarish.Audit.Application;

namespace Vitarish.Audit.Server;

internal sealed class PostgresAuditStorageSizeReader(AuditDbContext dbContext) : IAuditStorageSizeReader
{
    internal const string StorageSizeSql = "SELECT pg_table_size('\"Audit\".\"SecurityAuditEvents\"') + pg_indexes_size('\"Audit\".\"SecurityAuditEvents\"') AS \"Value\"";

    public async Task<long> GetStorageSizeBytesAsync(CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
            return 0;

        return await dbContext.Database.SqlQueryRaw<long>(StorageSizeSql).SingleAsync(cancellationToken);
    }
}
