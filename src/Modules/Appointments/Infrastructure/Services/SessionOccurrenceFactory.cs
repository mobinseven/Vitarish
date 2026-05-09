namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionOccurrenceFactory(AppointmentsDbContext dbContext) : ISessionOccurrenceFactory
{
    private const string WeeklyRecurrenceId = "weekly";

    public async Task<EntityResult<Guid>> GetOrCreateAsync(Guid sessionId, DateOnly date, CancellationToken cancellationToken)
    {
        if (sessionId == Guid.Empty)
        {
            return EntityResult<Guid>.Failure(EntityError.Validation("Session is required.", new Dictionary<string, string>
            {
                [nameof(sessionId)] = "Session is required."
            }));
        }

        Session? session = await dbContext.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session is null)
        {
            return EntityResult<Guid>.Failure(EntityError.NotFound("Session not found.", nameof(sessionId)));
        }

        EntityResult validation = ValidateSessionDate(session, date);
        if (validation.IsFailure)
        {
            return EntityResult<Guid>.Failure([.. validation.Errors]);
        }

        SessionOccurrence? existing = await dbContext.SessionOccurrences
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.SessionId == sessionId && o.Date == date, cancellationToken);

        if (existing is not null)
        {
            return EntityResult<Guid>.Success(existing.Id);
        }

        SessionOccurrence occurrence = CreateOccurrence(session, date);
        dbContext.SessionOccurrences.Add(occurrence);

        try
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return EntityResult<Guid>.Success(occurrence.Id);
        }
        catch (DbUpdateException exception) when (IsDuplicateSessionOccurrenceException(exception))
        {
            dbContext.Entry(occurrence).State = EntityState.Detached;

            SessionOccurrence? concurrentOccurrence = await dbContext.SessionOccurrences
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.SessionId == sessionId && o.Date == date, cancellationToken);

            return concurrentOccurrence is not null
                ? EntityResult<Guid>.Success(concurrentOccurrence.Id)
                : EntityResult<Guid>.Failure(EntityError.Conflict("Session occurrence could not be created."));
        }
    }

    private static EntityResult ValidateSessionDate(Session session, DateOnly date)
    {
        if (session.Status != SessionStatus.Active)
        {
            return EntityResult.Failure(EntityError.Conflict("This session is not open for reservations."));
        }

        if (date.DayOfWeek != session.DayOfWeek)
        {
            return EntityResult.Failure(EntityError.Validation("Occurrence date must match the session day of week.", new Dictionary<string, string>
            {
                [nameof(date)] = "Occurrence date must match the session day of week."
            }));
        }

        return EntityResult.Success();
    }

    private static SessionOccurrence CreateOccurrence(Session session, DateOnly date) => new()
    {
        SessionId = session.Id,
        Date = date,
        StartTime = session.StartTime,
        EndTime = session.EndTime,
        Capacity = session.TotalSeats,
        Metadata = BuildMetadata(session),
        IsCancelled = false
    };

    private static Dictionary<string, object?> BuildMetadata(Session session)
    {
        Dictionary<string, object?> metadata = new(session.Metadata);
        metadata["recurrenceId"] = WeeklyRecurrenceId;
        return metadata;
    }

    private static bool IsDuplicateSessionOccurrenceException(DbUpdateException exception)
    {
        string message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("IX_SessionOccurrences_SessionId_Date", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
    }
}
