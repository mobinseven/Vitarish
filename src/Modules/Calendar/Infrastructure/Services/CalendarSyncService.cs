using Microsoft.Extensions.Logging;
using Vitarish.Calendar.Application.Events;
using Vitarish.Calendar.Infrastructure.Helpers;
using Vitarish.Calendar.Infrastructure.Repositories;

namespace Vitarish.Calendar.Infrastructure.Services;

/// <summary>
/// Keeps the local database aligned with Radicale by diffing ETags and syncing changed objects.
/// </summary>
[RegisterTransient]
internal class CalendarSyncService(
    IEntityRepository<CalendarAccount, CalendarAccountDescriptor> accountRepository,
    CalendarObjectMappingRepository mappingRepository,
    OccurrenceProjectionRepository projectionRepository,
    CalendarSyncLedgerRepository ledgerRepository,
    IRadicaleAdapter radicaleAdapter,
    ICalendarHelper calendarHelper,
    ICalendarNotificationPublisher notificationPublisher,
    ILogger<CalendarSyncService> logger)
    : ICalendarSyncService
{
    public async Task SyncAccountAsync(Guid accountId, CancellationToken ct)
    {
        CalendarAccount? account = await accountRepository.GetAsync(accountId, ct);
        if (account is null || !account.IsActive)
        {
            return;
        }

        CalendarSyncLedger ledger = new()
        {
            CalendarAccountId = accountId,
            SyncDirection = SyncDirection.Inbound,
            StartedAt = DateTimeOffset.UtcNow,
            Status = SyncStatus.Running
        };
        CalendarSyncLedger? savedLedger = await ledgerRepository.AddAsync(ledger, ct);

        int processed = 0;
        int conflicts = 0;
        int failures = 0;
        string? lastError = null;

        try
        {
            IReadOnlyList<(string url, string eTag)> remote =
                await radicaleAdapter.ListCollectionObjectsAsync(account.RadicaleCollectionUrl, ct);

            IReadOnlyList<CalendarObjectMapping> local = await mappingRepository.GetAsync(
                new CalendarObjectMappingDescriptor { CalendarAccountId = accountId }, ct);

            Dictionary<string, CalendarObjectMapping> localByUrl = local
                .ToDictionary(m => m.RadicaleResourceUrl, StringComparer.OrdinalIgnoreCase);

            List<string> changedUrls = [];

            foreach ((string url, string remoteETag) in remote)
            {
                if (!localByUrl.TryGetValue(url, out CalendarObjectMapping? localMapping) ||
                    localMapping.ETag != remoteETag)
                {
                    changedUrls.Add(url);
                }
            }

            foreach (string url in changedUrls)
            {
                try
                {
                    (string iCalData, string eTag) = await radicaleAdapter.GetCalendarObjectAsync(url, ct);

                    CalendarObjectMapping? existing = localByUrl.GetValueOrDefault(url);
                    if (existing is not null)
                    {
                        existing.ETag = eTag;
                        existing.LastSyncAt = DateTimeOffset.UtcNow;
                        await mappingRepository.UpdateAsync(existing, ct);
                        await RebuildOccurrenceProjectionAsync(existing.Id, DateTimeOffset.UtcNow.AddDays(180), ct);
                    }

                    processed++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to sync calendar object at '{Url}'.", url);
                    failures++;
                    lastError = ex.Message;
                }
            }

            if (changedUrls.Count > 0)
            {
                await notificationPublisher.PublishAsync(
                    new ExternalCalendarChangedEvent(accountId, changedUrls), ct);
            }

            account.LastSyncAt = DateTimeOffset.UtcNow;
            account.SyncStatus = failures == 0 ? SyncStatus.Succeeded : SyncStatus.PartialFailure;
            await accountRepository.UpdateAsync(account, ct);

            if (savedLedger is not null)
            {
                savedLedger.CompletedAt = DateTimeOffset.UtcNow;
                savedLedger.ItemsProcessed = processed;
                savedLedger.Conflicts = conflicts;
                savedLedger.Failures = failures;
                savedLedger.LastError = lastError;
                savedLedger.Status = failures == 0 ? SyncStatus.Succeeded : SyncStatus.PartialFailure;
                await ledgerRepository.UpdateAsync(savedLedger, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sync failed for account '{AccountId}'.", accountId);

            if (savedLedger is not null)
            {
                savedLedger.CompletedAt = DateTimeOffset.UtcNow;
                savedLedger.Failures = failures + 1;
                savedLedger.LastError = ex.Message;
                savedLedger.Status = SyncStatus.Failed;
                await ledgerRepository.UpdateAsync(savedLedger, ct);
            }

            account.SyncStatus = SyncStatus.Failed;
            await accountRepository.UpdateAsync(account, ct);
        }
    }

    public async Task SyncAllAccountsAsync(CancellationToken ct)
    {
        IReadOnlyList<CalendarAccount> accounts = await accountRepository.GetAsync(
            new CalendarAccountDescriptor { IsActive = true }, ct);

        foreach (CalendarAccount account in accounts)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await SyncAccountAsync(account.Id, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sync failed for account '{AccountId}'.", account.Id);
            }
        }
    }

    public async Task RebuildOccurrenceProjectionAsync(Guid mappingId, DateTimeOffset horizonEnd, CancellationToken ct)
    {
        CalendarObjectMapping? mapping = await mappingRepository.GetAsync(mappingId, ct);
        if (mapping is null)
        {
            return;
        }

        CalendarAccount? account = await accountRepository.GetAsync(mapping.CalendarAccountId, ct);
        if (account is null || string.IsNullOrEmpty(mapping.RadicaleResourceUrl))
        {
            return;
        }

        (string iCalData, string _) = await radicaleAdapter.GetCalendarObjectAsync(mapping.RadicaleResourceUrl, ct);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End, string RecurrenceId)> occurrences =
            calendarHelper.ExpandOccurrences(iCalData, now, horizonEnd);

        DateTimeOffset projectedAt = DateTimeOffset.UtcNow;

        List<OccurrenceProjection> projections = [
            .. occurrences.Select(o => new OccurrenceProjection
            {
                MappingId = mappingId,
                RecurrenceId = o.RecurrenceId,
                OccurrenceStart = o.Start,
                OccurrenceEnd = o.End,
                IsException = false,
                IsCancelled = false,
                ProjectedAt = projectedAt,
                HorizonEnd = horizonEnd
            })
        ];

        if (projections.Count > 0)
        {
            await projectionRepository.AddRangeAsync(projections, ct);
        }

        await projectionRepository.DeleteByHorizonAsync(mappingId, projectedAt, ct);
    }

    public async Task RescanAndRebuildAsync(Guid accountId, CancellationToken ct)
    {
        await SyncAccountAsync(accountId, ct);

        IReadOnlyList<CalendarObjectMapping> mappings = await mappingRepository.GetAsync(
            new CalendarObjectMappingDescriptor { CalendarAccountId = accountId }, ct);

        foreach (CalendarObjectMapping mapping in mappings)
        {
            ct.ThrowIfCancellationRequested();
            await RebuildOccurrenceProjectionAsync(mapping.Id, DateTimeOffset.UtcNow.AddDays(180), ct);
        }
    }
}
