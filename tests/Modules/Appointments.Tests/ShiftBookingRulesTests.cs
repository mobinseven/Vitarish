namespace Appointments.Tests;

public class ShiftBookingRulesTests
{
    [Fact]
    public void IsBookingOpen_ShouldReturnFalseBeforeBookingOpenWindow()
    {
        Shift shift = CreateActiveShift(new DateOnly(2026, 1, 15));
        shift.BookingOpensBefore = 7; // opens 7 days before occurrence

        // 2026-01-08 opens the window; now is one day before → not yet open
        bool result = ShiftBookingRules.IsBookingOpen(
            shift,
            new DateOnly(2026, 1, 15),
            new DateTime(2026, 1, 7, 12, 0, 0, DateTimeKind.Utc));

        _ = result.Should().BeFalse();
    }

    [Fact]
    public void IsBookingOpen_ShouldReturnFalseForClosedDatePolicy()
    {
        DateOnly shiftDate = new(2026, 1, 15);
        Shift shift = CreateActiveShift(shiftDate);
        ShiftBookingPolicy closedPolicy = new() { Date = shiftDate, Closed = true };

        bool result = ShiftBookingRules.IsBookingOpen(
            shift,
            shiftDate,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            closedPolicy);

        _ = result.Should().BeFalse();
    }

    [Fact]
    public void IsBookingOpen_ShouldReturnFalseForInvalidShiftTimeZone()
    {
        Shift shift = CreateActiveShift(new DateOnly(2026, 1, 15));
        shift.TimeZone = "Invalid/Zone";

        bool result = ShiftBookingRules.IsBookingOpen(
            shift,
            new DateOnly(2026, 1, 15),
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        _ = result.Should().BeFalse();
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
