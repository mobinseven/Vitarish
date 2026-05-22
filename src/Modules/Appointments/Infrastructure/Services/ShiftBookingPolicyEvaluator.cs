using Vitarish.Appointments.Domain.Services;

namespace Vitarish.Appointments.Infrastructure.Services;

public static class ShiftBookingPolicyEvaluator
{
    public static bool IsBookingOpen(Shift shift, DateOnly date)
    {
        ShiftBookingPolicy? datePolicy = ResolvePolicy(shift, date);
        return ShiftBookingRules.IsBookingOpen(shift, date, DateTimeOffset.UtcNow, datePolicy);
    }

    public static bool CanCancel(Shift shift, Reservation reservation)
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

    public static bool CanReschedule(Shift shift, Reservation reservation)
    {
        ShiftBookingPolicy? datePolicy = ResolvePolicy(shift, reservation.Date);
        return datePolicy?.AllowRescheduling ?? false;
    }

    public static bool RequiresApproval(Shift shift, DateOnly date)
    {
        ShiftBookingPolicy? datePolicy = ResolvePolicy(shift, date);
        return datePolicy?.RequiresApproval ?? false;
    }

    public static bool NeedsPartyCapacityCheck(Shift shift, DateOnly date, string? reservingPartyId)
    {
        if (string.IsNullOrWhiteSpace(reservingPartyId))
        {
            return false;
        }

        ShiftBookingPolicy? datePolicy = ResolvePolicy(shift, date);
        return datePolicy?.MaxReservationsPerParty is not null;
    }

    public static bool HasCapacityForParty(Shift shift, DateOnly date, string? reservingPartyId, int currentReservationCount)
    {
        if (!NeedsPartyCapacityCheck(shift, date, reservingPartyId))
        {
            return true;
        }

        ShiftBookingPolicy? datePolicy = ResolvePolicy(shift, date);
        return currentReservationCount < datePolicy!.MaxReservationsPerParty!.Value;
    }

    public static int GetEffectiveCapacity(Shift shift, DateOnly date)
    {
        ShiftBookingPolicy? datePolicy = ResolvePolicy(shift, date);
        return datePolicy?.Capacity ?? shift.TotalSeats;
    }

    public static TimeSpan GetEffectiveEstimatedSessionLength(Shift shift, DateOnly date, TimeSpan? overrideEstimatedSessionLength = null)
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
