using Microsoft.Data.SqlClient;

namespace Vitarish.Contents.Infrastructure.Data;

internal static class ContentsDatabaseExceptions
{
    private const int SqlServerDuplicateKey = 2601;
    private const int SqlServerUniqueConstraintViolation = 2627;

    public static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException && IsSqlServerUniqueConstraintViolation(sqlException))
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
}
