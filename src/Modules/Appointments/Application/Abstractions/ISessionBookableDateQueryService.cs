namespace Vitarish.Appointments.Application.Abstractions;

public interface ISessionBookableDateQueryService
{
    Task<IReadOnlyList<SessionReservationReadDto>> GetBookableDatesAsync(Guid sessionId, SessionDateRangeQuery range, CancellationToken cancellationToken);
}
