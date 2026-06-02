namespace Appointments.Tests;

public class ShiftBookingPolicyEvaluatorTests
{
    [Fact]
    public void GetEffectiveEstimatedSessionLength_ShouldReturnOverride()
    {
        Shift shift = CreateShift(TimeOnly.Parse("09:00"), TimeOnly.Parse("10:00"), 4);
        DateOnly date = FutureDate(DayOfWeek.Tuesday);

        TimeSpan result = ShiftBookingPolicyEvaluator.GetEffectiveEstimatedSessionLength(shift, date, [], TimeSpan.FromMinutes(5));

        _ = result.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void GetEffectiveEstimatedSessionLength_ShouldReturnPolicyValueWhenOverrideNotProvided()
    {
        DateOnly date = FutureDate(DayOfWeek.Tuesday);
        Shift shift = CreateShift(TimeOnly.Parse("09:00"), TimeOnly.Parse("10:00"), 4);
        IReadOnlyList<ShiftBookingPolicy> policies = [new ShiftBookingPolicy
        {
            ShiftId = shift.Id,
            Date = date,
            EstimatedSessionLength = TimeSpan.FromMinutes(15)
        }];

        TimeSpan result = ShiftBookingPolicyEvaluator.GetEffectiveEstimatedSessionLength(shift, date, policies);

        _ = result.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void GetEffectiveEstimatedSessionLength_ShouldFallbackToRoundedDurationDividedByCapacity()
    {
        DateOnly date = FutureDate(DayOfWeek.Tuesday);
        Shift shift = CreateShift(TimeOnly.Parse("09:00"), TimeOnly.Parse("10:00"), 7);

        TimeSpan result = ShiftBookingPolicyEvaluator.GetEffectiveEstimatedSessionLength(shift, date, []);

        _ = result.Should().Be(TimeSpan.FromMinutes(9));
    }

    private static Shift CreateShift(TimeOnly startTime, TimeOnly endTime, int totalSeats) => new()
    {
        Id = Guid.NewGuid(),
        Status = ShiftStatus.Active,
        DayOfWeek = DayOfWeek.Tuesday,
        StartTime = startTime,
        EndTime = endTime,
        TimeZone = "UTC",
        TotalSeats = totalSeats
    };

    private static DateOnly FutureDate(DayOfWeek dayOfWeek)
    {
        DateOnly start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        int days = ((int)dayOfWeek - (int)start.DayOfWeek + 7) % 7;
        return start.AddDays(days == 0 ? 7 : days);
    }
}
