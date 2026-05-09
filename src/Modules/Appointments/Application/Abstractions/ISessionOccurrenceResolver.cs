namespace Vitarish.Appointments.Application.Abstractions;

public interface ISessionOccurrenceResolver
{
    Task<IReadOnlyList<SessionOccurrenceReadDto>> ResolveOccurrencesAsync(Guid sessionId, SessionDateRangeQuery range, CancellationToken cancellationToken);
    Task<SessionOccurrenceReadDto?> GetOccurrenceAsync(Guid sessionId, DateOnly date, CancellationToken cancellationToken);
    Task SyncOccurrencesAsync(Guid sessionId, CancellationToken cancellationToken);
}
