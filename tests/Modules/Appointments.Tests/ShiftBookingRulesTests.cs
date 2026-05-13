namespace Appointments.Tests;

public class ShiftBookingRulesTests
{
    [Fact]
    public void IsBookingOpen_ShouldUseShiftTimeZoneForBookingWindow()
    {
        Shift shift = CreateActiveShift(new DateOnly(2026, 1, 15));
        shift.TimeZone = "America/New_York";
        shift.BookingPolicy = new ShiftBookingPolicy
        {
            BookingOpensBefore = TimeSpan.FromDays(7)
        };

        bool result = ShiftBookingRules.IsBookingOpen(
            shift,
            new DateOnly(2026, 1, 15),
            new DateTimeOffset(2026, 1, 8, 12, 0, 0, TimeSpan.Zero));

        result.Should().BeFalse();
    }

    [Fact]
    public void IsBookingOpen_ShouldUseProvidedClosedDateSet()
    {
        DateOnly shiftDate = new(2026, 1, 15);
        Shift shift = CreateActiveShift(shiftDate);
        shift.BookingPolicy = new ShiftBookingPolicy();
        HashSet<DateOnly> closedDates = [shiftDate];

        bool result = ShiftBookingRules.IsBookingOpen(
            shift,
            shiftDate,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            closedDates);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsBookingOpen_ShouldReturnFalseForInvalidShiftTimeZone()
    {
        Shift shift = CreateActiveShift(new DateOnly(2026, 1, 15));
        shift.TimeZone = "Invalid/Zone";

        bool result = ShiftBookingRules.IsBookingOpen(
            shift,
            new DateOnly(2026, 1, 15),
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        result.Should().BeFalse();
    }

    private static Shift CreateActiveShift(DateOnly date) => new()
    {
        Status = ShiftStatus.Active,
        DayOfWeek = date.DayOfWeek,
        StartTime = new TimeOnly(10, 0),
        EndTime = new TimeOnly(11, 0),
        TimeZone = "UTC"
    };
}
