namespace Vitarish.Appointments.Application.Abstractions;

public interface IShiftBookingPolicyManager
{
    Task<IReadOnlyList<ShiftBookingPolicyReadDto>> GetByShiftAsync(Guid shiftId, CancellationToken cancellationToken);

    Task<EntityResult<ShiftBookingPolicyReadDto>> UpsertPolicyAsync(ShiftBookingPolicyWriteDto dto, CancellationToken cancellationToken);

    Task<EntityResult> RemovePolicyAsync(Guid shiftId, Guid policyId, CancellationToken cancellationToken);
}
