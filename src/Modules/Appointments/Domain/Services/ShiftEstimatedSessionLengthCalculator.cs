namespace Vitarish.Appointments.Domain.Services;

public static class ShiftEstimatedSessionLengthCalculator
{
    public static TimeSpan Calculate(TimeOnly startTime, TimeOnly endTime, int capacity)
    {
        TimeSpan duration = endTime.ToTimeSpan() - startTime.ToTimeSpan();
        if (duration <= TimeSpan.Zero)
        {
            duration = duration.Add(TimeSpan.FromDays(1));
        }

        if (duration <= TimeSpan.Zero)
        {
            return TimeSpan.FromMinutes(1);
        }

        int effectiveCapacity = Math.Max(1, capacity);
        int roundedMinutes = Math.Max(1, (int)Math.Round(duration.TotalMinutes / effectiveCapacity, MidpointRounding.AwayFromZero));
        return TimeSpan.FromMinutes(roundedMinutes);
    }
}
