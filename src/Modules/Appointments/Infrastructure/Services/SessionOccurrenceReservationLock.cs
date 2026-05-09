namespace Vitarish.Appointments.Infrastructure.Services;

internal class SessionOccurrenceReservationLock(AppointmentsDbContext dbContext) : ISessionOccurrenceReservationLock
{
    public async Task<EntityResult<SessionOccurrence>> LockAsync(Guid occurrenceId, CancellationToken cancellationToken)
    {
        SessionOccurrence? occurrence = dbContext.Database.IsSqlServer()
            ? await LockSqlServerOccurrenceAsync(occurrenceId, cancellationToken)
            : await LoadOccurrenceAsync(occurrenceId, cancellationToken);

        return occurrence is null
            ? EntityResult<SessionOccurrence>.Failure(EntityError.NotFound("Session occurrence not found.", nameof(occurrenceId)))
            : EntityResult<SessionOccurrence>.Success(occurrence);
    }

    private async Task<SessionOccurrence?> LockSqlServerOccurrenceAsync(Guid occurrenceId, CancellationToken cancellationToken) =>
        await dbContext.SessionOccurrences
            .FromSqlInterpolated($"SELECT * FROM [SessionOccurrences] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {occurrenceId}")
            .AsNoTracking()
            .Include(o => o.Session)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<SessionOccurrence?> LoadOccurrenceAsync(Guid occurrenceId, CancellationToken cancellationToken) =>
        await dbContext.SessionOccurrences
            .AsNoTracking()
            .Include(o => o.Session)
            .FirstOrDefaultAsync(o => o.Id == occurrenceId, cancellationToken);
}
