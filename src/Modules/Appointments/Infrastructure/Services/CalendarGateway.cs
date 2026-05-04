using Vitarish.Calendar.Application.Abstractions;
using Vitarish.Calendar.Domain.Dto;
using Vitarish.Calendar.Domain.Enums;
using Vitarish.Calendar.Domain.Models;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class CalendarGateway(ICalendarObjectService calendarObjectService) : ICalendarGateway
{
    public async Task<Guid> DefineSessionTimeAsync(
        Guid sessionId, string title, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
    {
        string iCalData = BuildICalData(title, start, end, sessionId.ToString());

        DomainCalendarRecordWriteDto record = new()
        {
            Category = CalendarObjectCategory.Appointment,
            TenantEntityId = sessionId.ToString()
        };

        CalendarObjectMappingReadDto mapping = await calendarObjectService.CreateEventAsync(record, iCalData, cancellationToken);
        return mapping.Id;
    }

    public async Task<Guid> DefineRecurringSessionTimeAsync(
        Guid sessionId, string title, DateTimeOffset start, DateTimeOffset end,
        RecurrenceInfo recurrence, CancellationToken cancellationToken)
    {
        DomainCalendarRecordWriteDto record = new()
        {
            Category = CalendarObjectCategory.Appointment,
            TenantEntityId = sessionId.ToString()
        };

        CalendarObjectMappingReadDto mapping = await calendarObjectService.CreateRecurringEventAsync(record, recurrence, cancellationToken);
        return mapping.Id;
    }

    public Task<IReadOnlyList<(string RecurrenceId, DateTimeOffset Start, DateTimeOffset End)>> GetUpcomingOccurrencesAsync(
        Guid calendarMappingId, TimeRangeQuery range, CancellationToken cancellationToken)
    {
        // GetCalendarViewAsync requires a calendar account ID, not a mapping ID.
        // Full Calendar integration requires resolving the account ID from the mapping,
        // which is handled separately. Return empty until that lookup is wired in.
        IReadOnlyList<(string, DateTimeOffset, DateTimeOffset)> empty = [];
        return Task.FromResult(empty);
    }

    public async Task CancelOccurrenceAsync(Guid calendarMappingId, string recurrenceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(recurrenceId))
        {
            await calendarObjectService.CancelEventAsync(calendarMappingId, cancellationToken);
        }
        else
        {
            string patchedIcal = BuildCancelledOccurrenceICalData(recurrenceId);
            await calendarObjectService.ApplyOccurrenceEditAsync(
                calendarMappingId, recurrenceId, OccurrenceEditScope.ThisOccurrence, patchedIcal, cancellationToken);
        }
    }

    private static string BuildICalData(string title, DateTimeOffset start, DateTimeOffset end, string uid)
    {
        string startStr = start.UtcDateTime.ToString("yyyyMMddTHHmmssZ");
        string endStr = end.UtcDateTime.ToString("yyyyMMddTHHmmssZ");
        return $"BEGIN:VCALENDAR\r\nVERSION:2.0\r\nBEGIN:VEVENT\r\nUID:{uid}\r\nSUMMARY:{title}\r\nDTSTART:{startStr}\r\nDTEND:{endStr}\r\nEND:VEVENT\r\nEND:VCALENDAR";
    }

    private static string BuildCancelledOccurrenceICalData(string recurrenceId)
        => $"BEGIN:VCALENDAR\r\nVERSION:2.0\r\nBEGIN:VEVENT\r\nSTATUS:CANCELLED\r\nRECURRENCE-ID:{recurrenceId}\r\nEND:VEVENT\r\nEND:VCALENDAR";
}
