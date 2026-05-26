
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using System.Data.Common;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Requests.Infrastructure.Data;

public sealed class RequestContextInterceptor(IRequestContext ctx) : DbConnectionInterceptor, IInterceptor
{
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (connection is NpgsqlConnection npgsql && ctx.OwnerId is not null)
        {
            await using NpgsqlCommand cmd = npgsql.CreateCommand();
            cmd.CommandText = "SELECT set_config('app.owner_id', @o, false), set_config('app.user_id', @u, false);";
            _ = cmd.Parameters.AddWithValue("@o", ctx.OwnerId);
            _ = cmd.Parameters.AddWithValue("@u", (object?)ctx.UserId ?? DBNull.Value);
            _ = await cmd.ExecuteScalarAsync(cancellationToken);
        }
    }
}
