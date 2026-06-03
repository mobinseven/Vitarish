
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using System.Data.Common;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Requests.Infrastructure.Data;

public sealed class RequestContextInterceptor(IRequestContext ctx) : DbConnectionInterceptor, IInterceptor
{
    public override async ValueTask<InterceptionResult> ConnectionClosingAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        if (connection is NpgsqlConnection npgsql)
        {

            if (ctx.OwnerId is not null)
            {
                await using NpgsqlCommand cmd = npgsql.CreateCommand();
                cmd.CommandText = "SELECT set_config('app.owner_id', '', false)";
                _ = await cmd.ExecuteNonQueryAsync();
            }

            if (ctx.UserId is not null)
            {
                await using NpgsqlCommand cmd = npgsql.CreateCommand();
                cmd.CommandText = "SELECT set_config('app.user_id', '', false)";
                _ = await cmd.ExecuteNonQueryAsync();
            }

        }
        return await base.ConnectionClosingAsync(connection, eventData, result);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (connection is not NpgsqlConnection npgsql)
        {
            return;
        }

        await using NpgsqlCommand cmd = npgsql.CreateCommand();
        var setParams = new List<string>();

        if (ctx.OwnerId is not null)
        {
            setParams.Add("SELECT set_config('app.owner_id', @ownerId, false)");
            _ = cmd.Parameters.AddWithValue("@ownerId", ctx.OwnerId);
        }

        if (ctx.UserId is not null)
        {
            setParams.Add("SELECT set_config('app.user_id', @userId, false)");
            _ = cmd.Parameters.AddWithValue("@userId", ctx.UserId);
        }

        if (setParams.Any())
        {
            cmd.CommandText = string.Join("; ", setParams);
            _ = await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
