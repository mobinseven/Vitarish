using Npgsql;

namespace Vitarish.Entities.Server.Data;

/// <summary>
/// Shared helper for detecting database-level constraint violations.
/// </summary>
public static class DatabaseExceptions
{
    // PostgreSQL error code for unique_violation
    private const string UniqueViolation = "23505";

    public static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException pgException && pgException.SqlState == UniqueViolation)
            {
                return true;
            }
        }

        return false;
    }
}
