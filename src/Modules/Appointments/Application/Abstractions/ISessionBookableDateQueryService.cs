namespace Vitarish.Appointments.Application.Abstractions;

public interface ISessionBookableDateQueryService
{
    Task<IReadOnlyList<SessionOccurrenceReadDto>> GetBookableDatesAsync(Guid sessionId, SessionDateRangeQuery range, CancellationToken cancellationToken);
}
