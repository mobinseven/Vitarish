using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace Vitarish.Appointments.Infrastructure.Data;

internal static class AppointmentsDatabaseExceptions
{
    private const int SqlServerDuplicateKey = 2601;
    private const int SqlServerUniqueConstraintViolation = 2627;
    private const int SqlitePrimaryKeyConstraint = 1555;
    private const int SqliteUniqueConstraint = 2067;

    public static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException && IsSqlServerUniqueConstraintViolation(sqlException))
            {
                return true;
            }

            if (current is SqliteException sqliteException && IsSqliteUniqueConstraintViolation(sqliteException))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSqlServerUniqueConstraintViolation(SqlException exception)
    {
        foreach (SqlError error in exception.Errors)
        {
            if (error.Number is SqlServerDuplicateKey or SqlServerUniqueConstraintViolation)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSqliteUniqueConstraintViolation(SqliteException exception) =>
        exception.SqliteExtendedErrorCode is SqlitePrimaryKeyConstraint or SqliteUniqueConstraint;
}
