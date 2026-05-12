namespace Vitarish.Appointments.Application.Abstractions;

/// <summary>
/// Projects scheduled session occurrences into availability read models for read-only consumers.
/// </summary>
public interface ISessionOccurrenceQueryService
{
    Task<IReadOnlyList<SessionReservationReadDto>> GetBookableDatesAsync(Guid sessionId, SessionDateRangeQuery range, CancellationToken cancellationToken);
}
