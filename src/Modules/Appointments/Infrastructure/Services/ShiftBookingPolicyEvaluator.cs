using Vitarish.Appointments.Domain.Services;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ShiftBookingPolicyEvaluator
{
    public bool IsBookingOpen(Shift shift, DateOnly date)
    {
        ShiftBookingPolicy? datePolicy = ResolvePolicy(shift, date);
        return ShiftBookingRules.IsBookingOpen(shift, date, DateTimeOffset.UtcNow, datePolicy);
    }

    public bool CanCancel(Shift shift, Reservation reservation)
    {
        ShiftBookingPolicy? datePolicy = ResolvePolicy(shift, reservation.Date);
        TimeSpan? deadline = datePolicy?.CancellationDeadline;
        if (deadline is null)
        {
            return true;
        }

        DateTimeOffset cutoff = ToDateTimeOffset(reservation.Date, shift.StartTime) - deadline.Value;
        return DateTimeOffset.UtcNow < cutoff;
    }

    public bool CanReschedule(Shift shift, Reservation reservation)
    {
        ShiftBookingPolicy? datePolicy = ResolvePolicy(shift, reservation.Date);
        return datePolicy?.AllowRescheduling ?? false;
    }

    public bool RequiresApproval(Shift shift, DateOnly date)
    {
        ShiftBookingPolicy? datePolicy = ResolvePolicy(shift, date);
        return datePolicy?.RequiresApproval ?? false;
    }

    public bool NeedsPartyCapacityCheck(Shift shift, DateOnly date, string? reservingPartyId)
    {
        if (string.IsNullOrWhiteSpace(reservingPartyId))
        {
            return false;
        }

        ShiftBookingPolicy? datePolicy = ResolvePolicy(shift, date);
        return datePolicy?.MaxReservationsPerParty is not null;
    }

    public bool HasCapacityForParty(Shift shift, DateOnly date, string? reservingPartyId, int currentReservationCount)
    {
        if (!NeedsPartyCapacityCheck(shift, date, reservingPartyId))
        {
            return true;
        }

        ShiftBookingPolicy? datePolicy = ResolvePolicy(shift, date);
        return currentReservationCount < datePolicy!.MaxReservationsPerParty!.Value;
    }

    public int GetEffectiveCapacity(Shift shift, DateOnly date)
    {
        ShiftBookingPolicy? datePolicy = ResolvePolicy(shift, date);
        return datePolicy?.Capacity ?? shift.TotalSeats;
    }

    public TimeSpan GetEffectiveEstimatedSessionLength(Shift shift, DateOnly date, TimeSpan? overrideEstimatedSessionLength = null)
    {
        if (overrideEstimatedSessionLength.HasValue)
        {
            return overrideEstimatedSessionLength.Value;
        }

        ShiftBookingPolicy? datePolicy = ResolvePolicy(shift, date);
        if (datePolicy?.EstimatedSessionLength is { } policyEstimatedSessionLength)
        {
            return policyEstimatedSessionLength;
        }

        int capacity = datePolicy?.Capacity ?? shift.TotalSeats;
        return ShiftEstimatedSessionLengthCalculator.Calculate(shift.StartTime, shift.EndTime, capacity);
    }

    private static ShiftBookingPolicy? ResolvePolicy(Shift shift, DateOnly date)
        => shift.BookingPolicies.FirstOrDefault(p => p.Date == date);

    private static DateTimeOffset ToDateTimeOffset(DateOnly date, TimeOnly time)
        => new(date.ToDateTime(time), TimeSpan.Zero);
}
