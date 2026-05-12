namespace Appointments.Tests;

public class SessionBookingRulesTests
{
    [Fact]
    public void IsBookingOpen_ShouldUseSessionTimeZoneForBookingWindow()
    {
        Session session = CreateActiveSession(new DateOnly(2026, 1, 15));
        session.TimeZone = "America/New_York";
        session.BookingPolicy = new SessionBookingPolicy
        {
            BookingOpensBefore = TimeSpan.FromDays(7)
        };

        bool result = SessionBookingRules.IsBookingOpen(
            session,
            new DateOnly(2026, 1, 15),
            new DateTimeOffset(2026, 1, 8, 12, 0, 0, TimeSpan.Zero));

        result.Should().BeFalse();
    }

    [Fact]
    public void IsBookingOpen_ShouldUseProvidedClosedDateSet()
    {
        DateOnly sessionDate = new(2026, 1, 15);
        Session session = CreateActiveSession(sessionDate);
        session.BookingPolicy = new SessionBookingPolicy();
        HashSet<DateOnly> closedDates = [sessionDate];

        bool result = SessionBookingRules.IsBookingOpen(
            session,
            sessionDate,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            closedDates);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsBookingOpen_ShouldReturnFalseForInvalidSessionTimeZone()
    {
        Session session = CreateActiveSession(new DateOnly(2026, 1, 15));
        session.TimeZone = "Invalid/Zone";

        bool result = SessionBookingRules.IsBookingOpen(
            session,
            new DateOnly(2026, 1, 15),
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        result.Should().BeFalse();
    }

    private static Session CreateActiveSession(DateOnly date) => new()
    {
        Status = SessionStatus.Active,
        DayOfWeek = date.DayOfWeek,
        StartTime = new TimeOnly(10, 0),
        EndTime = new TimeOnly(11, 0),
        TimeZone = "UTC"
    };
}
