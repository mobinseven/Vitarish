namespace Vitarish.Appointments.Application.Abstractions;

public interface IBookingPolicyEngine
{
    Task<bool> IsBookingOpenAsync(Guid sessionOccurrenceId, CancellationToken cancellationToken);
    Task<bool> CanCancelAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<bool> CanRescheduleAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<bool> HasCapacityForPartyAsync(Guid sessionOccurrenceId, string? reservingPartyId, CancellationToken cancellationToken);
}
