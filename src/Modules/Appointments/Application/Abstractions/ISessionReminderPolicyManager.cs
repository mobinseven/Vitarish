namespace Vitarish.Appointments.Application.Abstractions;

public interface ISessionReminderPolicyManager
{
    Task<IReadOnlyList<SessionReminderPolicyReadDto>> GetBySessionAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<EntityResult<SessionReminderPolicyReadDto>> AddPolicyAsync(SessionReminderPolicyWriteDto dto, CancellationToken cancellationToken);

    Task<EntityResult<SessionReminderPolicyReadDto>> UpdatePolicyAsync(SessionReminderPolicyWriteDto dto, CancellationToken cancellationToken);

    Task<EntityResult> RemovePolicyAsync(Guid policyId, CancellationToken cancellationToken);
}
