namespace Vitarish.Appointments.Application.Abstractions;

public interface ITemporaryHoldManager
{
    Task<EntityResult<Guid>> CreateHoldAsync(Guid shiftId, DateOnly date, string? reservingPartyId, TimeSpan duration, CancellationToken cancellationToken);
    Task<EntityResult> ConfirmHoldAsync(Guid holdId, CancellationToken cancellationToken);
    Task<EntityResult> ReleaseHoldAsync(Guid holdId, CancellationToken cancellationToken);
    Task ExpireStaleHoldsAsync(CancellationToken cancellationToken);
}
