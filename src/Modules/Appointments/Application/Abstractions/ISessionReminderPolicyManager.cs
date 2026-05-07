using Vitarish.Entities.Server.Models;

namespace Vitarish.Appointments.Application.Abstractions;

public interface ISessionReminderPolicyManager : IEntityService<SessionReminderPolicyReadDto, SessionReminderPolicyWriteDto, SessionReminderPolicyDescriptorDto>
{
}
