using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Transactions;
using Vitarish.Clinic.Application.Commands.Patients;
using Vitarish.Clinic.Infrastructure.Data;

namespace Vitarish.Clinic.Infrastructure.Handlers;

internal sealed class PatientViewRefreshCommandHandler(
    ClinicDbContext dbContext,
    ILogger<PatientViewRefreshCommandHandler> logger) : IRequestHandler<PatientViewRefreshCommand>
{
    public async Task Handle(PatientViewRefreshCommand request, CancellationToken cancellationToken)
    {
        if (request.Concurrently && (dbContext.Database.CurrentTransaction is not null || Transaction.Current is not null))
        {
            throw new InvalidOperationException("Cannot refresh the patient materialized view concurrently within an active transaction block.");
        }

        string concurrencyClause = request.Concurrently ? " CONCURRENTLY" : string.Empty;
        string sql = $"REFRESH MATERIALIZED VIEW{concurrencyClause} \"Clinic\".\"mv_Patients\";";

        try
        {
            _ = await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidOperationException or DbUpdateException or DbException)
        {
            logger.LogError(exception, "Failed to refresh Sea patient materialized view with concurrently={Concurrently}.", request.Concurrently);
            throw new InvalidOperationException("Failed to refresh the patient materialized view. Verify that the view exists and has the required unique index before using concurrent refresh.", exception);
        }
    }
}
