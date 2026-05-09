namespace Vitarish.Appointments.Infrastructure.Services;

internal interface ISessionOccurrenceReservationLock
{
    Task<EntityResult<SessionOccurrence>> LockAsync(Guid occurrenceId, CancellationToken cancellationToken);
}
