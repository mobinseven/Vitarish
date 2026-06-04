namespace Vitarish.Appointments.Infrastructure.Services;

internal static class ShiftBookingRules
{
    internal static bool IsBookingOpen(
        Shift shift,
        DateOnly date,
        DateTime now,
        ShiftBookingPolicy? datePolicy = null)
    {
        if (shift.Status != ShiftStatus.Active)
        {
            return false;
        }

        if (datePolicy?.Closed == true)
        {
            return false;
        }

        if (!TryGetSlotStart(shift, date, out DateTime slotStart))
        {
            return false;
        }

        if (now >= slotStart)
        {
            return false;
        }

        int? effectiveOpensBefore = datePolicy?.BookingOpensBefore ?? shift.BookingOpensBefore;
        int? effectiveCloseBefore = datePolicy?.BookingCloseBefore ?? shift.BookingCloseBefore;

        if (effectiveOpensBefore.HasValue)
        {
            DateTime opensAt = GetUtcShiftDateStart(date).AddDays(-effectiveOpensBefore.Value);
            if (now < opensAt)
            {
                return false;
            }
        }

        if (effectiveCloseBefore.HasValue)
        {
            DateTime closesAt = GetUtcShiftDateStart(date).AddDays(-effectiveCloseBefore.Value);
            if (now >= closesAt)
            {
                return false;
            }
        }

        return true;
    }

    private static DateTime GetUtcShiftDateStart(DateOnly date)
        => DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    private static bool TryGetSlotStart(Shift shift, DateOnly date, out DateTime slotStart)
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
            slotStart = TimeZoneInfo.ConvertTimeToUtc(dateTime, timeZone);
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
