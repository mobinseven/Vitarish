namespace Vitarish.Appointments.Application.Abstractions;

public interface ISessionReminderPolicyRepository
    : IEntityRepository<SessionReminderPolicy, SessionReminderPolicyDescriptor>
{
    Task<IReadOnlyList<SessionReminderPolicy>> GetBySessionIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SessionReminderPolicy>> GetEnabledBySessionIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken);
}
