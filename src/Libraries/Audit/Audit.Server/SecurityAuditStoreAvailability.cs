using System.Data.Common;
using Npgsql;

namespace Vitarish.Audit.Server;

/// <summary>
/// Distinguishes a store that is unavailable from an event the store refused: only connectivity and
/// resource exhaustion belong to an outage, while constraint, length and authorization errors come from
/// one bad event and must not inflate the outage summary.
/// </summary>
internal static class SecurityAuditStoreAvailability
{
    private static readonly string[] TransientSqlStateClasses = ["08", "53"];

    internal static bool IsUnavailable(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is NpgsqlException { IsTransient: true })
                return true;
            if (current is TimeoutException)
                return true;
            if (current is DbException { SqlState: { Length: >= 2 } sqlState }
                && TransientSqlStateClasses.Contains(sqlState[..2], StringComparer.Ordinal))
                return true;
        }

        return false;
    }
}
