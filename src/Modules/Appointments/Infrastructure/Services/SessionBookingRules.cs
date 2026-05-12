namespace Vitarish.Appointments.Infrastructure.Services;

internal static class SessionBookingRules
{
    internal static bool IsBookingOpen(Session session, DateOnly date, DateTimeOffset now)
    {
        if (session.Status != SessionStatus.Active)
        {
            return false;
        }

        SessionBookingPolicy? policy = session.BookingPolicy;

        if (policy?.ClosedDates.Any(cd => cd.Date == date) == true)
        {
            return false;
        }

        DateTimeOffset slotStart = ToDateTimeOffset(date, session.StartTime);

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

    private static DateTimeOffset ToDateTimeOffset(DateOnly date, TimeOnly time)
        => new(date.ToDateTime(time), TimeSpan.Zero);
}
