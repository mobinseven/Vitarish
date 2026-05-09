using Vitarish.Calendar.Domain.Models;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionOccurrenceResolver(
    IEntityRepository<Session, SessionDescriptor> sessionRepository,
    IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor> occurrenceRepository,
    AppointmentsDbContext dbContext) : ISessionOccurrenceResolver
{
    private const string WeeklyRecurrenceId = "weekly";

    public async Task<IReadOnlyList<SessionOccurrenceReadDto>> ResolveOccurrencesAsync(
        Guid sessionId, TimeRangeQuery range, CancellationToken cancellationToken)
    {
        await SyncOccurrencesAsync(
            sessionId,
            DateOnly.FromDateTime(range.Start.DateTime),
            DateOnly.FromDateTime(range.End.DateTime),
            cancellationToken);

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
        DateOnly today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.DateTime);
        await SyncOccurrencesAsync(sessionId, today, today.AddYears(1), cancellationToken);
    }

    private async Task SyncOccurrencesAsync(Guid sessionId, DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken)
    {
        if (dateTo < dateFrom)
        {
            return;
        }

        Session? session = await sessionRepository.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return;
        }

        Dictionary<DateOnly, SessionOccurrence> existingOccurrences = await dbContext.SessionOccurrences
            .Where(o => o.SessionId == sessionId && o.Date >= dateFrom && o.Date <= dateTo)
            .ToDictionaryAsync(o => o.Date, cancellationToken);

        foreach (DateOnly date in GetScheduledDates(session.DayOfWeek, dateFrom, dateTo))
        {
            SessionOccurrence occurrence = CreateOccurrence(session, date);
            await ValidateOccurrenceForSessionAsync(occurrence, session, cancellationToken);

            if (!existingOccurrences.TryGetValue(date, out SessionOccurrence? existing))
            {
                dbContext.SessionOccurrences.Add(occurrence);
            }
            else
            {
                ApplyOccurrence(existing, occurrence);
            }
        }

        await SaveOccurrenceChangesAsync(sessionId, cancellationToken);
    }

    private static IEnumerable<DateOnly> GetScheduledDates(DayOfWeek dayOfWeek, DateOnly dateFrom, DateOnly dateTo)
    {
        int daysUntilScheduledDay = ((int)dayOfWeek - (int)dateFrom.DayOfWeek + 7) % 7;

        for (DateOnly date = dateFrom.AddDays(daysUntilScheduledDay); date <= dateTo; date = date.AddDays(7))
        {
            yield return date;
        }
    }

    private static SessionOccurrence CreateOccurrence(Session session, DateOnly date) => new()
    {
        SessionId = session.Id,
        Date = date,
        StartTime = session.StartTime,
        EndTime = session.EndTime,
        Capacity = session.TotalSeats,
        Metadata = BuildMetadata(session),
        IsCancelled = session.Status == SessionStatus.Cancelled
    };

    private static Dictionary<string, object?> BuildMetadata(Session session)
    {
        Dictionary<string, object?> metadata = new(session.Metadata);
        metadata["recurrenceId"] = WeeklyRecurrenceId;
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

        if (occurrence.Date.DayOfWeek != session.DayOfWeek)
        {
            throw new InvalidOperationException("Occurrence date must match the session day of week.");
        }

        return Task.CompletedTask;
    }

    private async Task SaveOccurrenceChangesAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateSessionOccurrenceException(exception))
        {
            foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<SessionOccurrence> entry in dbContext.ChangeTracker
                .Entries<SessionOccurrence>()
                .Where(entry => entry.Entity.SessionId == sessionId && entry.State == EntityState.Added))
            {
                entry.State = EntityState.Detached;
            }

            _ = await dbContext.SaveChangesAsync(cancellationToken);
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
