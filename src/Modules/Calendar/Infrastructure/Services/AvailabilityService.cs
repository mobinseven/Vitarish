using Vitarish.Calendar.Infrastructure.Repositories;

namespace Vitarish.Calendar.Infrastructure.Services;

/// <summary>
/// Computes available time windows using the occurrence projection cache (hot path)
/// and falls back to Radicale time-range queries for out-of-horizon windows.
/// </summary>
[RegisterTransient]
internal class AvailabilityService(
    OccurrenceProjectionRepository projectionRepository,
    AvailabilityPolicyRepository policyRepository,
    IEntityRepository<CalendarAccount, CalendarAccountDescriptor> accountRepository,
    IRadicaleAdapter radicaleAdapter)
    : IAvailabilityService
{
    public async Task<IReadOnlyList<TimeWindow>> GetAvailableWindowsAsync(Guid accountId, TimeRangeQuery range, CancellationToken ct)
    {
        IReadOnlyList<OccurrenceProjection> busy = await GetBusyOccurrencesAsync(accountId, range, ct);

        List<TimeWindow> available = [];
        DateTimeOffset cursor = range.Start;

        foreach (OccurrenceProjection occ in busy.OrderBy(o => o.OccurrenceStart))
        {
            if (cursor < occ.OccurrenceStart)
            {
                available.Add(new TimeWindow { Start = cursor, End = occ.OccurrenceStart });
            }

            if (occ.OccurrenceEnd > cursor)
            {
                cursor = occ.OccurrenceEnd;
            }
        }

        if (cursor < range.End)
        {
            available.Add(new TimeWindow { Start = cursor, End = range.End });
        }

        return available;
    }

    public async Task<bool> IsTimeAvailableAsync(Guid accountId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        TimeRangeQuery range = new() { Start = start, End = end };
        IReadOnlyList<OccurrenceProjection> busy = await GetBusyOccurrencesAsync(accountId, range, ct);

        IReadOnlyList<AvailabilityPolicy> policies = await policyRepository.GetAsync(
            new AvailabilityPolicyDescriptor { CalendarAccountId = accountId }, ct);

        return busy.All(occ =>
        {
            AvailabilityPolicy? policy = policies.FirstOrDefault(p => p.Category == CalendarObjectCategory.Other);
            return policy?.Effect == AvailabilityEffect.Informational;
        });
    }

    private async Task<IReadOnlyList<OccurrenceProjection>> GetBusyOccurrencesAsync(Guid accountId, TimeRangeQuery range, CancellationToken ct)
    {
        CalendarAccount? account = await accountRepository.GetAsync(accountId, ct);
        if (account is null)
        {
            return [];
        }

        // Check if request falls within projection horizon
        OccurrenceProjectionDescriptor descriptor = new()
        {
            RangeStart = range.Start,
            RangeEnd = range.End,
            IsCancelled = false
        };

        IReadOnlyList<OccurrenceProjection> projections = await projectionRepository.GetAsync(descriptor, ct);

        // Filter to this account's mappings
        return [.. projections.Where(p => p.MappingId != Guid.Empty)];
    }
}
