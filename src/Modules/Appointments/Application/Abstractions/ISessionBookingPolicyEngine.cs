namespace Vitarish.Appointments.Application.Abstractions;

public interface ISessionBookingPolicyEngine
{
    Task<bool> IsBookingOpenAsync(Guid sessionId, DateOnly date, CancellationToken cancellationToken);
    Task<bool> CanCancelAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<bool> CanRescheduleAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<bool> HasCapacityForPartyAsync(Guid sessionId, DateOnly date, string? reservingPartyId, CancellationToken cancellationToken);
    Task<bool> RequiresApprovalAsync(Guid sessionId, CancellationToken cancellationToken);
}
