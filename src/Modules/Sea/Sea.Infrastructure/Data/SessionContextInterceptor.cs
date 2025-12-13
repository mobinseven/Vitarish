
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Sea.Infrastructure.Data;

public sealed class SessionContextInterceptor(IRequestContext ctx) : DbConnectionInterceptor
{
    private const string SqlServerSetSessionContext = @"
EXEC sys.sp_set_session_context @key=N'owner_id', @value=@o;
EXEC sys.sp_set_session_context @key=N'user_id',  @value=@u;";


    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {

        if (connection is SqlConnection sql && ctx.OwnerId is not null)
        {
            using SqlCommand cmd = sql.CreateCommand();
            cmd.CommandText = SqlServerSetSessionContext;
            _ = cmd.Parameters.Add(new SqlParameter("@o", ctx.OwnerId));
            _ = cmd.Parameters.Add(new SqlParameter("@u", (object?)ctx.UserId ?? DBNull.Value));
            _ = await cmd.ExecuteNonQueryAsync(cancellationToken);
            return;
        }

        if (connection is SqliteConnection sqlite)
        {
            RegisterSqliteSessionFunctions(sqlite);
            return;
        }
    }

    private void RegisterSqliteSessionFunctions(SqliteConnection sqlite)
    {
        sqlite.CreateFunction<string, string?>(
            "session_context",
            key => key?.Equals("owner_id", StringComparison.OrdinalIgnoreCase) == true
                    ? ctx.OwnerId?.ToString()
                 : key?.Equals("user_id", StringComparison.OrdinalIgnoreCase) == true
                    ? ctx.UserId?.ToString()
                 : null,
            isDeterministic: false);

        sqlite.CreateFunction("owner_id", () => ctx.OwnerId?.ToString(), isDeterministic: false);
        sqlite.CreateFunction("user_id", () => ctx.UserId?.ToString(), isDeterministic: false);
    }
}
