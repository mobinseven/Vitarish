namespace Vitarish.Calendar.Application.Abstractions;

/// <summary>
/// Keeps the local database aligned with Radicale. Called by background jobs and the admin service.
/// </summary>
public interface ICalendarSyncService
{
    Task SyncAccountAsync(Guid accountId, CancellationToken ct);

    Task SyncAllAccountsAsync(CancellationToken ct);

    Task RebuildOccurrenceProjectionAsync(Guid mappingId, DateTimeOffset horizonEnd, CancellationToken ct);

    /// <summary>
    /// Full recovery: rescans the Radicale collection and rebuilds all projections for the account.
    /// </summary>
    Task RescanAndRebuildAsync(Guid accountId, CancellationToken ct);
}
