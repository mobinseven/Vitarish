using Microsoft.Data.SqlClient;

namespace Vitarish.Social.Infrastructure.Helpers;

internal static class SocialDatabaseExceptions
{
    public static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        if (exception.InnerException is SqlException sql)
        {
            return sql.Number is 2601 or 2627;
        }

        return false;
    }
}
