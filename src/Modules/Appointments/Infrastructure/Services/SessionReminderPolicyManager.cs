namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class SessionReminderPolicyManager(IMediator mediator)
    : EntityService<SessionReminderPolicy, SessionReminderPolicyDescriptor, SessionReminderPolicyReadDto, SessionReminderPolicyWriteDto, SessionReminderPolicyDescriptorDto>(
        mediator,
        policy => policy.ToDto(),
        policy => policy.ToDomain(),
        policy => policy.ToDomain()),
      ISessionReminderPolicyManager;
