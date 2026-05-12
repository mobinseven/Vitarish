namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionBookableDateQueryService(
    IEntityRepository<Session, SessionDescriptor> sessionRepository,
    AppointmentsDbContext dbContext) : ISessionBookableDateQueryService
{
    public async Task<IReadOnlyList<SessionReservationReadDto>> GetBookableDatesAsync(
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

        DateTimeOffset now = DateTimeOffset.UtcNow;
        HashSet<DateOnly> closedDates = session.BookingPolicy?.ClosedDates
            .Select(cd => cd.Date)
            .ToHashSet() ?? [];

        Dictionary<DateOnly, SessionReservation> existingByDate = await dbContext.Set<SessionReservation>()
            .AsNoTracking()
            .Where(sr => sr.SessionId == sessionId && sr.Date >= dateFrom && sr.Date <= dateTo)
            .ToDictionaryAsync(sr => sr.Date, cancellationToken);

        return [.. GetScheduledDates(session.DayOfWeek, dateFrom, dateTo)
            .Where(date => SessionBookingRules.IsBookingOpen(session, date, now, closedDates))
            .Select(date => existingByDate.TryGetValue(date, out SessionReservation? existing)
                ? existing.ToDto()
                : CreateProjectedReservation(session, date).ToDto())];
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

    private static SessionReservation CreateProjectedReservation(Session session, DateOnly date) => new()
    {
        SessionId = session.Id,
        Date = date,
        StartTime = session.StartTime,
        EndTime = session.EndTime,
        TotalSeats = session.TotalSeats,
        ReservationCount = 0,
        AvailableSeats = session.TotalSeats
    };
}
