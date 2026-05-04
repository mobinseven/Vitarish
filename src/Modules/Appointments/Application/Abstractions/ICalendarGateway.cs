using Vitarish.Calendar.Domain.Models;

namespace Vitarish.Appointments.Application.Abstractions;

public interface ICalendarGateway
{
    Task<Guid> DefineSessionTimeAsync(Guid sessionId, string title, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken);
    Task<Guid> DefineRecurringSessionTimeAsync(Guid sessionId, string title, DateTimeOffset start, DateTimeOffset end, RecurrenceInfo recurrence, CancellationToken cancellationToken);
    Task<IReadOnlyList<(string RecurrenceId, DateTimeOffset Start, DateTimeOffset End)>> GetUpcomingOccurrencesAsync(Guid calendarMappingId, TimeRangeQuery range, CancellationToken cancellationToken);
    Task CancelOccurrenceAsync(Guid calendarMappingId, string recurrenceId, CancellationToken cancellationToken);
}
