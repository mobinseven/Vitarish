namespace Vitarish.Appointments.Infrastructure.Services;

internal static class ShiftBookingRules
{
    internal static bool IsBookingOpen(
        Shift shift,
        DateOnly date,
        DateTimeOffset now,
        IReadOnlySet<DateOnly>? closedDates = null)
    {
        if (shift.Status != ShiftStatus.Active)
        {
            return false;
        }

        ShiftBookingPolicy? policy = shift.BookingPolicy;

        if (IsClosedDate(policy, closedDates, date))
        {
            return false;
        }

        if (!TryGetSlotStart(shift, date, out DateTimeOffset slotStart))
        {
            return false;
        }

        if (now >= slotStart)
        {
            return false;
        }

        if (policy is null)
        {
            return true;
        }

        if (policy.BookingOpensBefore.HasValue)
        {
            DateTimeOffset opensAt = slotStart - policy.BookingOpensBefore.Value;
            if (now < opensAt)
            {
                return false;
            }
        }

        if (policy.BookingCloseBefore.HasValue)
        {
            DateTimeOffset closesAt = slotStart - policy.BookingCloseBefore.Value;
            if (now >= closesAt)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsClosedDate(
        ShiftBookingPolicy? policy,
        IReadOnlySet<DateOnly>? closedDates,
        DateOnly date)
        => closedDates?.Contains(date)
            ?? policy?.ClosedDates.Any(cd => cd.Date == date)
            ?? false;

    private static bool TryGetSlotStart(Shift shift, DateOnly date, out DateTimeOffset slotStart)
    {
        slotStart = default;

        if (string.IsNullOrWhiteSpace(shift.TimeZone))
        {
            return false;
        }

        DateTime dateTime = DateTime.SpecifyKind(date.ToDateTime(shift.StartTime), DateTimeKind.Unspecified);

        try
        {
            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(shift.TimeZone);
            slotStart = new DateTimeOffset(dateTime, timeZone.GetUtcOffset(dateTime));
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
