namespace Vitarish.Calendar.Application.Abstractions;

/// <summary>
/// Business-level calendar object operations. Consuming modules call this to create, update,
/// and cancel events, and to retrieve rendered calendar views.
/// </summary>
public interface ICalendarObjectService
{
    Task<CalendarObjectMappingReadDto> CreateEventAsync(DomainCalendarRecordWriteDto dto, string iCalData, CancellationToken ct);

    Task<CalendarObjectMappingReadDto> CreateRecurringEventAsync(DomainCalendarRecordWriteDto dto, RecurrenceInfo recurrence, CancellationToken ct);

    Task UpdateEventAsync(Guid mappingId, DomainCalendarRecordWriteDto dto, string iCalData, CancellationToken ct);

    Task CancelEventAsync(Guid mappingId, CancellationToken ct);

    Task ApplyOccurrenceEditAsync(Guid mappingId, string recurrenceId, OccurrenceEditScope scope, string patchedICalData, CancellationToken ct);

    Task<CalendarViewDto> GetCalendarViewAsync(Guid accountId, TimeRangeQuery range, CancellationToken ct);
}
