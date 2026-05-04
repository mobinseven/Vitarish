using Microsoft.Extensions.Logging;
using Vitarish.Calendar.Application.Events;
using Vitarish.Calendar.Infrastructure.Helpers;
using Vitarish.Calendar.Infrastructure.Repositories;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Calendar.Infrastructure.Services;

/// <summary>
/// Orchestrates CalDAV object creation, update, cancellation, and occurrence edits.
/// </summary>
[RegisterTransient]
internal class CalendarObjectService(
    IEntityRepository<CalendarObjectMapping, CalendarObjectMappingDescriptor> mappingRepository,
    IEntityRepository<DomainCalendarRecord, DomainCalendarRecordDescriptor> recordRepository,
    IEntityRepository<CalendarAuditEntry, CalendarAuditEntryDescriptor> auditRepository,
    IEntityRepository<CalendarAccount, CalendarAccountDescriptor> accountRepository,
    IRadicaleAdapter radicaleAdapter,
    ICalendarHelper calendarHelper,
    ICalendarNotificationPublisher notificationPublisher,
    IRequestContext requestContext,
    ILogger<CalendarObjectService> logger)
    : ICalendarObjectService
{
    public async Task<CalendarObjectMappingReadDto> CreateEventAsync(DomainCalendarRecordWriteDto dto, string iCalData, CancellationToken ct)
    {
        CalendarAccount? account = await accountRepository.GetAsync(dto.MappingId, ct);

        if (account is null)
        {
            CalendarObjectMappingWriteDto writeDto = new()
            {
                CalendarAccountId = Guid.Empty,
                LocalObjectId = dto.Id.ToString(),
                LocalObjectType = dto.Category.ToString(),
                ICalendarUid = Guid.NewGuid().ToString(),
                ComponentType = CalendarComponentType.VEvent,
                Category = dto.Category,
                RadicaleResourceUrl = string.Empty
            };

            // Persist mapping and record without Radicale when no account is configured
            CalendarObjectMapping? mapping = await mappingRepository.AddAsync(writeDto.ToDomain(), ct);
            dto.MappingId = mapping!.Id;
            await recordRepository.AddAsync(dto.ToDomain(), ct);
            await WriteAuditAsync(mapping.Id, Guid.Empty, CalendarAuditAction.Created, null, iCalData, ct);
            return mapping.ToDto();
        }

        string objectName = Guid.NewGuid().ToString("N");
        string newETag = await radicaleAdapter.PutCalendarObjectAsync(account.RadicaleCollectionUrl, objectName, iCalData, null, ct);
        string resourceUrl = $"{account.RadicaleCollectionUrl.TrimEnd('/')}/{objectName}.ics";

        CalendarObjectMappingWriteDto mappingDto = new()
        {
            CalendarAccountId = account.Id,
            LocalObjectId = dto.Id.ToString(),
            LocalObjectType = dto.Category.ToString(),
            ICalendarUid = objectName,
            ETag = newETag,
            ComponentType = CalendarComponentType.VEvent,
            Category = dto.Category,
            RadicaleResourceUrl = resourceUrl,
            IsExternallyManaged = false
        };

        CalendarObjectMapping? createdMapping = await mappingRepository.AddAsync(mappingDto.ToDomain(), ct);
        dto.MappingId = createdMapping!.Id;
        await recordRepository.AddAsync(dto.ToDomain(), ct);
        await WriteAuditAsync(createdMapping.Id, account.Id, CalendarAuditAction.Created, null, iCalData, ct);

        return createdMapping.ToDto();
    }

    public async Task<CalendarObjectMappingReadDto> CreateRecurringEventAsync(DomainCalendarRecordWriteDto dto, RecurrenceInfo recurrence, CancellationToken ct)
    {
        CalendarAccount? account = await accountRepository.GetAsync(dto.MappingId, ct);

        string iCalData = calendarHelper.BuildRecurringVEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1),
            dto.Category.ToString(),
            recurrence,
            account?.DefaultTimeZone ?? "UTC");

        return await CreateEventAsync(dto, iCalData, ct);
    }

    public async Task UpdateEventAsync(Guid mappingId, DomainCalendarRecordWriteDto dto, string iCalData, CancellationToken ct)
    {
        CalendarObjectMapping? mapping = await mappingRepository.GetAsync(mappingId, ct);
        if (mapping is null)
        {
            return;
        }

        CalendarAccount? account = await accountRepository.GetAsync(mapping.CalendarAccountId, ct);

        string? newETag = null;
        if (account is not null && !string.IsNullOrEmpty(mapping.RadicaleResourceUrl))
        {
            newETag = await radicaleAdapter.PutCalendarObjectAsync(
                account.RadicaleCollectionUrl,
                mapping.ICalendarUid,
                iCalData,
                mapping.ETag,
                ct);
        }

        mapping.ETag = newETag ?? mapping.ETag;
        mapping.LastSyncAt = DateTimeOffset.UtcNow;
        await mappingRepository.UpdateAsync(mapping, ct);

        IReadOnlyList<DomainCalendarRecord> records = await recordRepository.GetAsync(
            new DomainCalendarRecordDescriptor { Id = mappingId }, ct);
        DomainCalendarRecord? record = records.FirstOrDefault();

        if (record is not null)
        {
            record.Category = dto.Category;
            record.BusinessStatus = dto.BusinessStatus;
            record.Visibility = dto.Visibility;
            record.OwnerId = dto.OwnerId;
            record.ProviderId = dto.ProviderId;
            record.ResourceId = dto.ResourceId;
            record.PatientId = dto.PatientId;
            await recordRepository.UpdateAsync(record, ct);
        }

        await WriteAuditAsync(mappingId, mapping.CalendarAccountId, CalendarAuditAction.Updated, null, iCalData, ct);
    }

    public async Task CancelEventAsync(Guid mappingId, CancellationToken ct)
    {
        CalendarObjectMapping? mapping = await mappingRepository.GetAsync(mappingId, ct);
        if (mapping is null)
        {
            return;
        }

        CalendarAccount? account = await accountRepository.GetAsync(mapping.CalendarAccountId, ct);

        if (account is not null && !string.IsNullOrEmpty(mapping.RadicaleResourceUrl))
        {
            (string iCalData, string _) = await radicaleAdapter.GetCalendarObjectAsync(mapping.RadicaleResourceUrl, ct);
            string cancelledICalData = calendarHelper.ApplyCancellation(iCalData);
            await radicaleAdapter.PutCalendarObjectAsync(
                account.RadicaleCollectionUrl,
                mapping.ICalendarUid,
                cancelledICalData,
                mapping.ETag,
                ct);
        }

        await WriteAuditAsync(mappingId, mapping.CalendarAccountId, CalendarAuditAction.Cancelled, null, null, ct);
        await notificationPublisher.PublishAsync(
            new OccurrenceCancelledEvent(mappingId, string.Empty, OccurrenceEditScope.AllOccurrences), ct);
    }

    public async Task ApplyOccurrenceEditAsync(Guid mappingId, string recurrenceId, OccurrenceEditScope scope, string patchedICalData, CancellationToken ct)
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

        (string masterICalData, string _) = await radicaleAdapter.GetCalendarObjectAsync(mapping.RadicaleResourceUrl, ct);

        switch (scope)
        {
            case OccurrenceEditScope.ThisOccurrence:
            {
                string updatedMaster = calendarHelper.ApplyThisOccurrenceEdit(masterICalData, recurrenceId, patchedICalData);
                string exceptionName = $"{mapping.ICalendarUid}-{recurrenceId}";
                await radicaleAdapter.PutCalendarObjectAsync(account.RadicaleCollectionUrl, exceptionName, patchedICalData, null, ct);
                await radicaleAdapter.PutCalendarObjectAsync(account.RadicaleCollectionUrl, mapping.ICalendarUid, updatedMaster, mapping.ETag, ct);
                break;
            }
            case OccurrenceEditScope.ThisAndFuture:
            {
                string truncatedMaster = calendarHelper.ApplyThisAndFutureEdit(masterICalData, recurrenceId, patchedICalData);
                await radicaleAdapter.PutCalendarObjectAsync(account.RadicaleCollectionUrl, mapping.ICalendarUid, truncatedMaster, mapping.ETag, ct);
                string newSeriesName = Guid.NewGuid().ToString("N");
                await radicaleAdapter.PutCalendarObjectAsync(account.RadicaleCollectionUrl, newSeriesName, patchedICalData, null, ct);
                break;
            }
            case OccurrenceEditScope.AllOccurrences:
                await radicaleAdapter.PutCalendarObjectAsync(account.RadicaleCollectionUrl, mapping.ICalendarUid, patchedICalData, mapping.ETag, ct);
                break;
        }

        await WriteAuditAsync(mappingId, mapping.CalendarAccountId, CalendarAuditAction.OccurrenceEdited, masterICalData, patchedICalData, ct);
        await notificationPublisher.PublishAsync(new OccurrenceCancelledEvent(mappingId, recurrenceId, scope), ct);
    }

    public async Task<CalendarViewDto> GetCalendarViewAsync(Guid accountId, TimeRangeQuery range, CancellationToken ct)
    {
        CalendarAccount? account = await accountRepository.GetAsync(accountId, ct);
        if (account is null)
        {
            return new CalendarViewDto { Range = range };
        }

        IReadOnlyList<(string url, string iCalData, string eTag)> results =
            await radicaleAdapter.QueryTimeRangeAsync(account.RadicaleCollectionUrl, range, ct);

        List<CalendarEventViewDto> events = [];

        foreach ((string url, string iCalData, string eTag) in results)
        {
            RecurrenceInfo recurrence = calendarHelper.ParseRecurrenceInfo(iCalData);
            IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End, string RecurrenceId)> occurrences =
                calendarHelper.ExpandOccurrences(iCalData, range.Start, range.End);

            foreach ((DateTimeOffset start, DateTimeOffset end, string recId) in occurrences)
            {
                IReadOnlyList<CalendarObjectMapping> mappings = await mappingRepository.GetAsync(
                    new CalendarObjectMappingDescriptor { CalendarAccountId = accountId }, ct);
                CalendarObjectMapping? mapping = mappings.FirstOrDefault(m => m.RadicaleResourceUrl == url);

                events.Add(new CalendarEventViewDto
                {
                    MappingId = mapping?.Id ?? Guid.Empty,
                    Start = start,
                    End = end,
                    Title = mapping?.Category.ToString() ?? string.Empty,
                    Category = mapping?.Category ?? CalendarObjectCategory.Other,
                    BusinessStatus = string.Empty,
                    IsRecurring = recurrence.IsRecurring,
                    RecurrenceId = recurrence.IsRecurring ? recId : null
                });
            }
        }

        return new CalendarViewDto { Range = range, Events = events };
    }

    private async Task WriteAuditAsync(
        Guid objectMappingId,
        Guid accountId,
        CalendarAuditAction action,
        string? oldValue,
        string? newValue,
        CancellationToken ct)
    {
        CalendarAuditEntry entry = new()
        {
            ObjectMappingId = objectMappingId,
            AccountId = accountId,
            Action = action,
            PerformedBy = requestContext.OwnerId ?? string.Empty,
            PerformedAt = DateTimeOffset.UtcNow,
            OldValue = oldValue,
            NewValue = newValue
        };

        await auditRepository.AddAsync(entry, ct);
    }
}
