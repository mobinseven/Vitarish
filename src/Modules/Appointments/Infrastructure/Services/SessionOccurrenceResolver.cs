using Vitarish.Calendar.Domain.Models;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionOccurrenceResolver(
    IEntityRepository<Session, SessionDescriptor> sessionRepository,
    IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor> occurrenceRepository,
    AppointmentsDbContext dbContext) : ISessionOccurrenceResolver
{
    public async Task<IReadOnlyList<SessionOccurrenceReadDto>> ResolveOccurrencesAsync(
        Guid sessionId, TimeRangeQuery range, CancellationToken cancellationToken)
    {
        DateOnly dateFrom = DateOnly.FromDateTime(range.Start.DateTime);
        DateOnly dateTo = DateOnly.FromDateTime(range.End.DateTime);

        if (dateTo < dateFrom)
        {
            return [];
        }

        Session? session = await sessionRepository.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return [];
        }

        SessionOccurrenceDescriptor descriptor = new()
        {
            SessionId = sessionId,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Skip = 0,
            Take = int.MaxValue
        };

        IReadOnlyList<SessionOccurrence> existingOccurrences = await occurrenceRepository.GetAsync(descriptor, cancellationToken);
        Dictionary<DateOnly, SessionOccurrence> existingByDate = existingOccurrences.ToDictionary(o => o.Date);

        return [.. GetScheduledDates(session.DayOfWeek, dateFrom, dateTo)
            .Select(date => existingByDate.TryGetValue(date, out SessionOccurrence? existing)
                ? existing.ToDto()
                : CreateProjectedOccurrence(session, date).ToDto())];
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

    public Task SyncOccurrencesAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    private static IEnumerable<DateOnly> GetScheduledDates(DayOfWeek dayOfWeek, DateOnly dateFrom, DateOnly dateTo)
    {
        int daysUntilScheduledDay = ((int)dayOfWeek - (int)dateFrom.DayOfWeek + 7) % 7;

        for (DateOnly date = dateFrom.AddDays(daysUntilScheduledDay); date <= dateTo; date = date.AddDays(7))
        {
            yield return date;
        }
    }

    private static SessionOccurrence CreateProjectedOccurrence(Session session, DateOnly date) => new()
    {
        SessionId = session.Id,
        Date = date,
        StartTime = session.StartTime,
        EndTime = session.EndTime,
        Capacity = session.TotalSeats,
        IsCancelled = session.Status == SessionStatus.Cancelled
    };
}
