namespace Vitarish.Appointments.Application.Abstractions;

public interface IShiftReminderPolicyManager
{
    Task<IReadOnlyList<ShiftReminderPolicyReadDto>> GetByShiftAsync(Guid shiftId, CancellationToken cancellationToken);

    Task<EntityResult<ShiftReminderPolicyReadDto>> AddPolicyAsync(ShiftReminderPolicyWriteDto dto, CancellationToken cancellationToken);

    Task<EntityResult<ShiftReminderPolicyReadDto>> UpdatePolicyAsync(ShiftReminderPolicyWriteDto dto, CancellationToken cancellationToken);

    Task<EntityResult> RemovePolicyAsync(Guid shiftId, Guid policyId, CancellationToken cancellationToken);
}
