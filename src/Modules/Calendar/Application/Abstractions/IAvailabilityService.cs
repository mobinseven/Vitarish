namespace Vitarish.Calendar.Application.Abstractions;

/// <summary>
/// Provides availability information for consuming modules (e.g. Appointments session booking).
/// </summary>
public interface IAvailabilityService
{
    Task<IReadOnlyList<TimeWindow>> GetAvailableWindowsAsync(Guid accountId, TimeRangeQuery range, CancellationToken ct);

    Task<bool> IsTimeAvailableAsync(Guid accountId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct);
}
