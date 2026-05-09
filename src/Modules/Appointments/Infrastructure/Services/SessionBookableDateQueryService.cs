namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionBookableDateQueryService(
    IEntityRepository<Session, SessionDescriptor> sessionRepository,
    IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor> occurrenceRepository) : ISessionBookableDateQueryService
{
    public async Task<IReadOnlyList<SessionOccurrenceReadDto>> GetBookableDatesAsync(
        Guid sessionId, SessionDateRangeQuery range, CancellationToken cancellationToken)
    {
        if (!TryNormalizeRange(range, out DateOnly dateFrom, out DateOnly dateTo))
        {
            return [];
        }

        Session? session = await sessionRepository.GetAsync(sessionId, cancellationToken);
        if (session is null || session.Status != SessionStatus.Active)
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

    private static bool TryNormalizeRange(SessionDateRangeQuery range, out DateOnly dateFrom, out DateOnly dateTo)
    {
        dateFrom = range.DateFrom;
        dateTo = range.DateTo;

        return dateFrom != default && dateTo != default && dateTo >= dateFrom;
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
        Metadata = new Dictionary<string, object?>(session.Metadata),
        IsCancelled = false
    };
}
