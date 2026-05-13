namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ShiftBookableDateQueryService(
    IEntityRepository<Shift, ShiftDescriptor> shiftRepository,
    IShiftReservationViewRepository reservationViewRepository) : IShiftBookableDateQueryService
{
    public async Task<IReadOnlyList<ShiftReservationReadDto>> GetBookableDatesAsync(
        Guid shiftId, ShiftDateRangeQuery range, CancellationToken cancellationToken)
    {
        if (!TryNormalizeRange(range, out DateOnly dateFrom, out DateOnly dateTo))
        {
            return [];
        }

        Shift? shift = await shiftRepository.GetAsync(shiftId, cancellationToken);
        if (shift is null || shift.Status != ShiftStatus.Active)
        {
            return [];
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        HashSet<DateOnly> closedDates = shift.BookingPolicy?.ClosedDates
            .Select(cd => cd.Date)
            .ToHashSet() ?? [];

        Dictionary<DateOnly, ShiftReservation> existingByDate =
            (await reservationViewRepository.GetAsync([shiftId], dateFrom, dateTo, cancellationToken))
            .ToDictionary(sr => sr.Date);

        return [.. GetScheduledDates(shift.DayOfWeek, dateFrom, dateTo)
            .Where(date => ShiftBookingRules.IsBookingOpen(shift, date, now, closedDates))
            .Select(date => existingByDate.TryGetValue(date, out ShiftReservation? existing)
                ? existing.ToDto()
                : CreateProjectedReservation(shift, date).ToDto())];
    }

    public async Task<IReadOnlyList<BookableShiftSlot>> GetBookableSlotsAsync(
        ShiftDescriptorDto descriptor, ShiftDateRangeQuery range, CancellationToken cancellationToken)
    {
        if (!TryNormalizeRange(range, out DateOnly dateFrom, out DateOnly dateTo))
        {
            return [];
        }

        // Pagination from the caller is intentionally overridden: the calendar must evaluate
        // all matching shifts to project a complete date range view.
        ShiftDescriptor domainDescriptor = (descriptor with { Skip = 0, Take = int.MaxValue }).ToDomain();
        IReadOnlyList<Shift> shifts = await shiftRepository.GetAsync(domainDescriptor, cancellationToken);
        if (shifts.Count == 0)
        {
            return [];
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        IReadOnlyList<Guid> shiftIds = [.. shifts.Select(s => s.Id)];

        Dictionary<Guid, Dictionary<DateOnly, ShiftReservation>> reservationsByShift =
            (await reservationViewRepository.GetAsync(shiftIds, dateFrom, dateTo, cancellationToken))
            .GroupBy(sr => sr.ShiftId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(sr => sr.Date));

        List<BookableShiftSlot> slots = [];

        foreach (Shift shift in shifts)
        {
            HashSet<DateOnly> closedDates = shift.BookingPolicy?.ClosedDates
                .Select(cd => cd.Date)
                .ToHashSet() ?? [];

            Dictionary<DateOnly, ShiftReservation> existingByDate =
                reservationsByShift.GetValueOrDefault(shift.Id) ?? [];

            foreach (DateOnly date in GetScheduledDates(shift.DayOfWeek, dateFrom, dateTo)
                .Where(date => ShiftBookingRules.IsBookingOpen(shift, date, now, closedDates)))
            {
                ShiftReservationReadDto occurrence = existingByDate.TryGetValue(date, out ShiftReservation? existing)
                    ? existing.ToDto()
                    : CreateProjectedReservation(shift, date).ToDto();

                slots.Add(new BookableShiftSlot(shift.ToDto(), occurrence));
            }
        }

        return slots;
    }

    private static bool TryNormalizeRange(ShiftDateRangeQuery range, out DateOnly dateFrom, out DateOnly dateTo)
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

    private static ShiftReservation CreateProjectedReservation(Shift shift, DateOnly date) => new()
    {
        ShiftId = shift.Id,
        Date = date,
        StartTime = shift.StartTime,
        EndTime = shift.EndTime,
        TotalSeats = shift.TotalSeats,
        ReservationCount = 0,
        AvailableSeats = shift.TotalSeats
    };
}
