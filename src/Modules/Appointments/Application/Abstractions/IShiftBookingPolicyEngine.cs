namespace Vitarish.Appointments.Application.Abstractions;

public interface IShiftBookingPolicyEngine
{
    Task<bool> IsBookingOpenAsync(Guid shiftId, DateOnly date, CancellationToken cancellationToken);
    Task<bool> CanCancelAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<bool> CanRescheduleAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<bool> HasCapacityForPartyAsync(Guid shiftId, DateOnly date, string? reservingPartyId, CancellationToken cancellationToken);
    Task<bool> RequiresApprovalAsync(Guid shiftId, CancellationToken cancellationToken);
}
