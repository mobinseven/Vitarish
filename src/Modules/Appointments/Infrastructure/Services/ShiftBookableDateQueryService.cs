using Vitarish.Appointments.Domain.Mappers;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ShiftBookableDateQueryService(
    IEntityRepository<Shift, ShiftDescriptor> shiftRepository,
    IShiftReservationViewRepository reservationViewRepository,
    AppointmentsDbContext dbContext) : IShiftBookableDateQueryService
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

        IReadOnlyList<ShiftBookingPolicy> policies = await LoadPoliciesAsync([shiftId], cancellationToken);
        IReadOnlyList<ShiftReservation> existing = await reservationViewRepository.GetAsync([shiftId], dateFrom, dateTo, cancellationToken);

        Dictionary<DateOnly, ShiftReservation> existingByDate = existing
            .ToDictionary(sr => sr.Date);

        DateTime now = DateTime.UtcNow;

        return [.. GetScheduledDates(shift.DayOfWeek, dateFrom, dateTo)
            .Select(date => (date, policy: ResolvePolicy(policies, date)))
            .Where(t => ShiftBookingRules.IsBookingOpen(shift, t.date, now, t.policy))
            .Select(t => existingByDate.TryGetValue(t.date, out ShiftReservation? existing)
                ? existing.ToRead() with
                {
                    IsClosed = t.policy?.Closed == true,
                    EstimatedSessionLength = ShiftBookingPolicyEvaluator.GetEffectiveEstimatedSessionLength(shift, t.date, policies)
                }
                : CreateProjectedReservation(shift, t.date, policies).ToRead() with
                {
                    IsClosed = t.policy?.Closed == true,
                    EstimatedSessionLength = ShiftBookingPolicyEvaluator.GetEffectiveEstimatedSessionLength(shift, t.date, policies)
                })];
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
        ShiftDescriptor domainDescriptor = (descriptor with { Skip = 0, Take = int.MaxValue }).ToDescriptor();
        IReadOnlyList<Shift> shifts = await shiftRepository.GetAsync(domainDescriptor, cancellationToken);
        if (shifts.Count == 0)
        {
            return [];
        }

        DateTime now = DateTime.UtcNow;
        IReadOnlyList<Guid> shiftIds = [.. shifts.Select(s => s.Id)];

        IReadOnlyList<ShiftBookingPolicy> allPolicies = await LoadPoliciesAsync(shiftIds, cancellationToken);
        IReadOnlyList<ShiftReservation> reservations = await reservationViewRepository.GetAsync(shiftIds, dateFrom, dateTo, cancellationToken);

        Dictionary<Guid, Dictionary<DateOnly, ShiftReservation>> reservationsByShift =
            reservations
            .GroupBy(sr => sr.ShiftId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(sr => sr.Date));

        List<BookableShiftSlot> slots = [];

        foreach (Shift shift in shifts)
        {
            IReadOnlyList<ShiftBookingPolicy> shiftPolicies = [.. allPolicies.Where(p => p.ShiftId == shift.Id)];
            Dictionary<DateOnly, ShiftReservation> existingByDate =
                reservationsByShift.GetValueOrDefault(shift.Id) ?? [];

            foreach ((DateOnly date, ShiftBookingPolicy? datePolicy) in GetScheduledDates(shift.DayOfWeek, dateFrom, dateTo)
                .Select(date => (date, policy: ResolvePolicy(shiftPolicies, date)))
                .Where(t => ShiftBookingRules.IsBookingOpen(shift, t.date, now, t.policy)))
            {
                ShiftReservationReadDto occurrence = existingByDate.TryGetValue(date, out ShiftReservation? existing)
                    ? existing.ToRead() with
                    {
                        IsClosed = datePolicy?.Closed == true,
                        EstimatedSessionLength = ShiftBookingPolicyEvaluator.GetEffectiveEstimatedSessionLength(shift, date, shiftPolicies)
                    }
                    : CreateProjectedReservation(shift, date, shiftPolicies).ToRead() with
                    {
                        EstimatedSessionLength = ShiftBookingPolicyEvaluator.GetEffectiveEstimatedSessionLength(shift, date, shiftPolicies)
                    };

                slots.Add(new BookableShiftSlot(shift.ToRead(), occurrence));
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

    private ShiftReservation CreateProjectedReservation(Shift shift, DateOnly date, IReadOnlyList<ShiftBookingPolicy> policies) => new()
    {
        ShiftId = shift.Id,
        Date = date,
        StartTime = shift.StartTime,
        EndTime = shift.EndTime,
        TotalSeats = ShiftBookingPolicyEvaluator.GetEffectiveCapacity(shift, date, policies),
        ReservationCount = 0,
        AvailableSeats = ShiftBookingPolicyEvaluator.GetEffectiveCapacity(shift, date, policies)
    };

    private static ShiftBookingPolicy? ResolvePolicy(IReadOnlyList<ShiftBookingPolicy> policies, DateOnly date)
        => policies.FirstOrDefault(p => p.Date == date);

    private async Task<IReadOnlyList<ShiftBookingPolicy>> LoadPoliciesAsync(
        IReadOnlyList<Guid> shiftIds, CancellationToken cancellationToken)
        => await dbContext.ShiftBookingPolicies
            .AsNoTracking()
            .Where(p => shiftIds.Contains(p.ShiftId))
            .ToListAsync(cancellationToken);
}
