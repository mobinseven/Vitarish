namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionOccurrenceFactory(AppointmentsDbContext dbContext) : ISessionOccurrenceFactory
{
    public async Task<EntityResult<Guid>> GetOrCreateAsync(Guid sessionId, DateOnly date, CancellationToken cancellationToken)
    {
        EntityResult<SessionOccurrence> occurrenceResult = await GetOrCreateOccurrenceAsync(sessionId, date, cancellationToken);
        if (occurrenceResult.IsFailure)
        {
            return EntityResult<Guid>.Failure([.. occurrenceResult.Errors]);
        }

        return occurrenceResult.Value is null
            ? EntityResult<Guid>.Failure(EntityError.Conflict("Session occurrence could not be created."))
            : EntityResult<Guid>.Success(occurrenceResult.Value.Id);
    }

    public async Task<EntityResult<SessionOccurrence>> GetOrCreateOccurrenceAsync(Guid sessionId, DateOnly date, CancellationToken cancellationToken)
    {
        if (sessionId == Guid.Empty)
        {
            return EntityResult<SessionOccurrence>.Failure(EntityError.Validation("Session is required.", new Dictionary<string, string>
            {
                [nameof(sessionId)] = "Session is required."
            }));
        }

        Session? session = await dbContext.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session is null)
        {
            return EntityResult<SessionOccurrence>.Failure(EntityError.NotFound("Session not found.", nameof(sessionId)));
        }

        EntityResult validation = ValidateSessionDate(session, date);
        if (validation.IsFailure)
        {
            return EntityResult<SessionOccurrence>.Failure([.. validation.Errors]);
        }

        SessionOccurrence? existing = await dbContext.SessionOccurrences
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.SessionId == sessionId && o.Date == date, cancellationToken);

        if (existing is not null)
        {
            existing.Session = session;
            return EntityResult<SessionOccurrence>.Success(existing);
        }

        SessionOccurrence occurrence = CreateOccurrence(session, date);
        dbContext.SessionOccurrences.Add(occurrence);

        try
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return EntityResult<SessionOccurrence>.Success(occurrence);
        }
        catch (DbUpdateException exception) when (AppointmentsDatabaseExceptions.IsUniqueConstraintViolation(exception))
        {
            dbContext.Entry(occurrence).State = EntityState.Detached;

            SessionOccurrence? concurrentOccurrence = await dbContext.SessionOccurrences
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.SessionId == sessionId && o.Date == date, cancellationToken);

            if (concurrentOccurrence is not null)
            {
                concurrentOccurrence.Session = session;
                return EntityResult<SessionOccurrence>.Success(concurrentOccurrence);
            }

            return EntityResult<SessionOccurrence>.Failure(EntityError.Conflict("Session occurrence could not be created."));
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
        Metadata = new Dictionary<string, object?>(session.Metadata),
        IsCancelled = false
    };

}
