using Vitarish.Appointments.Domain.Services;

namespace Vitarish.Appointments.Infrastructure.Services;

public static class ShiftBookingPolicyEvaluator
{
    public static bool IsBookingOpen(Shift shift, DateOnly date, IReadOnlyList<ShiftBookingPolicy> policies)
    {
        ShiftBookingPolicy? datePolicy = ResolvePolicy(policies, date);
        return ShiftBookingRules.IsBookingOpen(shift, date, DateTime.UtcNow, datePolicy);
    }

    public static bool CanCancel(Shift shift, Reservation reservation, IReadOnlyList<ShiftBookingPolicy> policies)
    {
        ShiftBookingPolicy? datePolicy = ResolvePolicy(policies, reservation.Date);
        TimeSpan? deadline = datePolicy?.CancellationDeadline;
        if (deadline is null)
        {
            return true;
        }

        DateTime cutoff = ToDateTime(shift, reservation.Date, shift.StartTime) - deadline.Value;
        return DateTime.UtcNow < cutoff;
    }

    public static bool CanReschedule(Shift shift, Reservation reservation, IReadOnlyList<ShiftBookingPolicy> policies)
    {
        ShiftBookingPolicy? datePolicy = ResolvePolicy(policies, reservation.Date);
        return datePolicy?.AllowRescheduling ?? false;
    }

    public static bool RequiresApproval(Shift shift, DateOnly date, IReadOnlyList<ShiftBookingPolicy> policies)
    {
        ShiftBookingPolicy? datePolicy = ResolvePolicy(policies, date);
        return datePolicy?.RequiresApproval ?? false;
    }

    public static bool NeedsPartyCapacityCheck(Shift shift, DateOnly date, string? reservingPartyId, IReadOnlyList<ShiftBookingPolicy> policies)
    {
        if (string.IsNullOrWhiteSpace(reservingPartyId))
        {
            return false;
        }

        ShiftBookingPolicy? datePolicy = ResolvePolicy(policies, date);
        return datePolicy?.MaxReservationsPerParty is not null;
    }

    public static bool HasCapacityForParty(Shift shift, DateOnly date, string? reservingPartyId, int currentReservationCount, IReadOnlyList<ShiftBookingPolicy> policies)
    {
        if (!NeedsPartyCapacityCheck(shift, date, reservingPartyId, policies))
        {
            return true;
        }

        ShiftBookingPolicy? datePolicy = ResolvePolicy(policies, date);
        return currentReservationCount < datePolicy!.MaxReservationsPerParty!.Value;
    }

    public static int GetEffectiveCapacity(Shift shift, DateOnly date, IReadOnlyList<ShiftBookingPolicy> policies)
    {
        ShiftBookingPolicy? datePolicy = ResolvePolicy(policies, date);
        return datePolicy?.Capacity ?? shift.TotalSeats;
    }

    public static TimeSpan GetEffectiveEstimatedSessionLength(Shift shift, DateOnly date, IReadOnlyList<ShiftBookingPolicy> policies, TimeSpan? overrideEstimatedSessionLength = null)
    {
        if (overrideEstimatedSessionLength.HasValue)
        {
            return overrideEstimatedSessionLength.Value;
        }

        ShiftBookingPolicy? datePolicy = ResolvePolicy(policies, date);
        if (datePolicy?.EstimatedSessionLength is { } policyEstimatedSessionLength)
        {
            return policyEstimatedSessionLength;
        }

        int capacity = datePolicy?.Capacity ?? shift.TotalSeats;
        return ShiftEstimatedSessionLengthCalculator.Calculate(shift.StartTime, shift.EndTime, capacity);
    }

    private static ShiftBookingPolicy? ResolvePolicy(IReadOnlyList<ShiftBookingPolicy> policies, DateOnly date)
        => policies.FirstOrDefault(p => p.Date == date);

    private static DateTime ToDateTime(Shift shift, DateOnly date, TimeOnly time)
    {
        DateTime localTime = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);

        if (string.IsNullOrWhiteSpace(shift.TimeZone))
        {
            return DateTime.SpecifyKind(localTime, DateTimeKind.Utc);
        }

        try
        {
            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(shift.TimeZone);
            return TimeZoneInfo.ConvertTimeToUtc(localTime, timeZone);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException)
        {
            return DateTime.SpecifyKind(localTime, DateTimeKind.Utc);
        }
    }
}
