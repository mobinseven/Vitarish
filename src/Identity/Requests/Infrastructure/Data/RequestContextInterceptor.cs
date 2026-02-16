
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Requests.Infrastructure.Data;

public sealed class RequestContextInterceptor(IRequestContext ctx) : DbConnectionInterceptor, IInterceptor
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
    }
}
