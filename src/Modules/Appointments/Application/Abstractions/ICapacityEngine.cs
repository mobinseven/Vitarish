namespace Vitarish.Appointments.Application.Abstractions;

public interface ICapacityEngine
{
    Task<int> GetEffectiveCapacityAsync(Guid sessionOccurrenceId, CancellationToken cancellationToken);
    Task<int> GetAvailableSeatsAsync(Guid sessionOccurrenceId, CancellationToken cancellationToken);
    Task<bool> IsAvailableAsync(Guid sessionOccurrenceId, CancellationToken cancellationToken);
}
