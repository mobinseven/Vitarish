namespace Vitarish.Appointments.Infrastructure.Services;

internal static class ShiftBookingRules
{
    internal static bool IsBookingOpen(
        Shift shift,
        DateOnly date,
        DateTimeOffset now,
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

        if (!TryGetSlotStart(shift, date, out DateTimeOffset slotStart))
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
            DateTimeOffset opensAt = slotStart.Date.AddDays(-effectiveOpensBefore.Value);
            if (now < opensAt)
            {
                return false;
            }
        }

        if (effectiveCloseBefore.HasValue)
        {
            DateTimeOffset closesAt = slotStart.Date.AddDays(-effectiveCloseBefore.Value);
            if (now >= closesAt)
            {
                return false;
            }
        }

        return true;
    }

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
