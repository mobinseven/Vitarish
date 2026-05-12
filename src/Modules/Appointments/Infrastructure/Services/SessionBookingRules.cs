namespace Vitarish.Appointments.Infrastructure.Services;

internal static class SessionBookingRules
{
    internal static bool IsBookingOpen(
        Session session,
        DateOnly date,
        DateTimeOffset now,
        IReadOnlySet<DateOnly>? closedDates = null)
    {
        if (session.Status != SessionStatus.Active)
        {
            return false;
        }

        SessionBookingPolicy? policy = session.BookingPolicy;

        if (IsClosedDate(policy, closedDates, date))
        {
            return false;
        }

        if (!TryGetSlotStart(session, date, out DateTimeOffset slotStart))
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
        SessionBookingPolicy? policy,
        IReadOnlySet<DateOnly>? closedDates,
        DateOnly date)
        => closedDates?.Contains(date)
            ?? policy?.ClosedDates.Any(cd => cd.Date == date)
            ?? false;

    private static bool TryGetSlotStart(Session session, DateOnly date, out DateTimeOffset slotStart)
    {
        slotStart = default;

        if (string.IsNullOrWhiteSpace(session.TimeZone))
        {
            return false;
        }

        DateTime dateTime = DateTime.SpecifyKind(date.ToDateTime(session.StartTime), DateTimeKind.Unspecified);

        try
        {
            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(session.TimeZone);
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
