using Vitarish.Calendar.Domain.Models;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionOccurrenceResolver(
    IEntityRepository<Session, SessionDescriptor> sessionRepository,
    IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor> occurrenceRepository,
    AppointmentsDbContext dbContext) : ISessionOccurrenceResolver
{
    private const string SingleOccurrenceRecurrenceId = "single";

    public async Task<IReadOnlyList<SessionOccurrenceReadDto>> ResolveOccurrencesAsync(
        Guid sessionId, TimeRangeQuery range, CancellationToken cancellationToken)
    {
        await SyncOccurrencesAsync(sessionId, cancellationToken);

        SessionOccurrenceDescriptor descriptor = new()
        {
            SessionId = sessionId,
            DateFrom = DateOnly.FromDateTime(range.Start.DateTime),
            DateTo = DateOnly.FromDateTime(range.End.DateTime),
            IsCancelled = false,
            Skip = 0,
            Take = int.MaxValue
        };

        IReadOnlyList<SessionOccurrence> occurrences = await occurrenceRepository.GetAsync(descriptor, cancellationToken);
        return [.. occurrences.Select(o => o.ToDto())];
    }

    public async Task<SessionOccurrenceReadDto?> GetOccurrenceAsync(
        Guid sessionId, DateOnly date, CancellationToken cancellationToken)
    {
        SessionOccurrence? occurrence = await dbContext.SessionOccurrences
            .AsNoTracking()
            .Include(o => o.Session)
            .FirstOrDefaultAsync(o => o.SessionId == sessionId && o.Date == date, cancellationToken);

        return occurrence?.ToDto();
    }

    public async Task SyncOccurrencesAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        Session? session = await sessionRepository.GetAsync(sessionId, cancellationToken);

        if (session?.StartAt is null || session.EndAt is null)
        {
            return;
        }

        SessionOccurrence occurrence = CreateOccurrence(session);
        await ValidateOccurrenceForSessionAsync(occurrence, session, cancellationToken);

        SessionOccurrence? existing = await dbContext.SessionOccurrences
            .FirstOrDefaultAsync(o => o.SessionId == sessionId && o.Date == occurrence.Date, cancellationToken);

        if (existing is null)
        {
            dbContext.SessionOccurrences.Add(occurrence);
        }
        else
        {
            ApplyOccurrence(existing, occurrence);
        }

        await SaveOccurrenceChangesAsync(occurrence.SessionId, occurrence.Date, cancellationToken);
    }

    private static SessionOccurrence CreateOccurrence(Session session) => new()
    {
        SessionId = session.Id,
        Date = DateOnly.FromDateTime(session.StartAt!.Value.DateTime),
        StartTime = TimeOnly.FromDateTime(session.StartAt.Value.DateTime),
        EndTime = TimeOnly.FromDateTime(session.EndAt!.Value.DateTime),
        Capacity = session.TotalSeats,
        Metadata = BuildMetadata(session),
        IsCancelled = session.Status == SessionStatus.Cancelled
    };

    private static Dictionary<string, object?> BuildMetadata(Session session)
    {
        Dictionary<string, object?> metadata = new(session.Metadata);
        metadata["recurrenceId"] = SingleOccurrenceRecurrenceId;
        if (session.CalendarMappingId is { } calendarMappingId)
        {
            metadata["calendarMappingId"] = calendarMappingId;
        }

        return metadata;
    }

    private static void ApplyOccurrence(SessionOccurrence target, SessionOccurrence source)
    {
        target.StartTime = source.StartTime;
        target.EndTime = source.EndTime;
        target.Capacity = source.Capacity;
        target.Metadata = source.Metadata;
        target.IsCancelled = source.IsCancelled;
    }

    private static Task ValidateOccurrenceForSessionAsync(
        SessionOccurrence occurrence, Session session, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (session.DayOfWeek is { } sessionDayOfWeek && occurrence.Date.DayOfWeek != sessionDayOfWeek)
        {
            throw new InvalidOperationException("Occurrence date must match the session day of week.");
        }

        return Task.CompletedTask;
    }

    private async Task SaveOccurrenceChangesAsync(Guid sessionId, DateOnly date, CancellationToken cancellationToken)
    {
        try
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateSessionOccurrenceException(exception))
        {
            foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<SessionOccurrence> entry in dbContext.ChangeTracker
                .Entries<SessionOccurrence>()
                .Where(entry => entry.State == EntityState.Added))
            {
                entry.State = EntityState.Detached;
            }

            SessionOccurrence? existing = await dbContext.SessionOccurrences
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.SessionId == sessionId && o.Date == date, cancellationToken);

            if (existing is null)
            {
                throw new InvalidOperationException("Concurrent occurrence creation failed to load the existing occurrence.", exception);
            }
        }
    }

    private static bool IsDuplicateSessionOccurrenceException(DbUpdateException exception)
    {
        string message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("IX_SessionOccurrences_SessionId_Date", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
    }
}
