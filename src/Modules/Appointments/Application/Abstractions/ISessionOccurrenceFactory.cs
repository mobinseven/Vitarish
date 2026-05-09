namespace Vitarish.Appointments.Application.Abstractions;

public interface ISessionOccurrenceFactory
{
    Task<EntityResult<Guid>> GetOrCreateAsync(Guid sessionId, DateOnly date, CancellationToken cancellationToken);
}
