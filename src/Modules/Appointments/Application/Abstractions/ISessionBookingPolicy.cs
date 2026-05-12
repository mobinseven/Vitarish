namespace Vitarish.Appointments.Application.Abstractions;

/// <summary>
/// Answers command-side booking policy decisions for reservation workflows.
/// </summary>
public interface ISessionBookingPolicy
{
    Task<bool> IsBookingOpenAsync(Guid sessionId, DateOnly date, CancellationToken cancellationToken);
    Task<bool> CanCancelAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<bool> CanRescheduleAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<bool> HasCapacityForPartyAsync(Guid sessionId, DateOnly date, string? reservingPartyId, CancellationToken cancellationToken);
    Task<bool> RequiresApprovalAsync(Guid sessionId, CancellationToken cancellationToken);
}
