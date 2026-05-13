namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionBookableDateQueryService(
    IEntityRepository<Session, SessionDescriptor> sessionRepository,
    ISessionReservationViewRepository reservationViewRepository) : ISessionBookableDateQueryService
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

        Dictionary<DateOnly, SessionReservation> existingByDate =
            (await reservationViewRepository.GetAsync([sessionId], dateFrom, dateTo, cancellationToken))
            .ToDictionary(sr => sr.Date);

        return [.. GetScheduledDates(session.DayOfWeek, dateFrom, dateTo)
            .Where(date => SessionBookingRules.IsBookingOpen(session, date, now, closedDates))
            .Select(date => existingByDate.TryGetValue(date, out SessionReservation? existing)
                ? existing.ToDto()
                : CreateProjectedReservation(session, date).ToDto())];
    }

    public async Task<IReadOnlyList<BookableSessionSlot>> GetBookableSlotsAsync(
        SessionDescriptorDto descriptor, SessionDateRangeQuery range, CancellationToken cancellationToken)
    {
        if (!TryNormalizeRange(range, out DateOnly dateFrom, out DateOnly dateTo))
        {
            return [];
        }

        // Pagination from the caller is intentionally overridden: the calendar must evaluate
        // all matching sessions to project a complete date range view.
        SessionDescriptor domainDescriptor = (descriptor with { Skip = 0, Take = int.MaxValue }).ToDomain();
        IReadOnlyList<Session> sessions = await sessionRepository.GetAsync(domainDescriptor, cancellationToken);
        if (sessions.Count == 0)
        {
            return [];
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        IReadOnlyList<Guid> sessionIds = [.. sessions.Select(s => s.Id)];

        Dictionary<Guid, Dictionary<DateOnly, SessionReservation>> reservationsBySession =
            (await reservationViewRepository.GetAsync(sessionIds, dateFrom, dateTo, cancellationToken))
            .GroupBy(sr => sr.SessionId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(sr => sr.Date));

        List<BookableSessionSlot> slots = [];

        foreach (Session session in sessions)
        {
            HashSet<DateOnly> closedDates = session.BookingPolicy?.ClosedDates
                .Select(cd => cd.Date)
                .ToHashSet() ?? [];

            Dictionary<DateOnly, SessionReservation> existingByDate =
                reservationsBySession.GetValueOrDefault(session.Id) ?? [];

            foreach (DateOnly date in GetScheduledDates(session.DayOfWeek, dateFrom, dateTo)
                .Where(date => SessionBookingRules.IsBookingOpen(session, date, now, closedDates)))
            {
                SessionReservationReadDto occurrence = existingByDate.TryGetValue(date, out SessionReservation? existing)
                    ? existing.ToDto()
                    : CreateProjectedReservation(session, date).ToDto();

                slots.Add(new BookableSessionSlot(session.ToDto(), occurrence));
            }
        }

        return slots;
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
