using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Domain.Surgery;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Clinic.Infrastructure.Data;

namespace Vitarish.Clinic.Infrastructure.Handlers;

internal sealed class SurgeryMaterializedViewRefresher(ClinicDbContext dbContext) : IProjectObjectMaterializedViewRefresher
{
    public string ObjectType => SurgeryObjectTypes.ObjectType;

    public async Task RefreshAsync(string tenantId, CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException("Materialized-view refresh must run outside an active transaction.");
        }

        DbConnection connection = dbContext.Database.GetDbConnection();
        bool closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = "REFRESH MATERIALIZED VIEW CONCURRENTLY \"Clinic\".\"mv_PatientSurgeries\";";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (closeConnection)
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }
    }
}
