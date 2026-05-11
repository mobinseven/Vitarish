using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Entities;

namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for SessionReminderPolicy owned entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Appointments.Domain.Dto.SessionReminderPolicyReadDto ToDto(global::Vitarish.Appointments.Domain.Entities.SessionReminderPolicy src);

    public partial global::Vitarish.Appointments.Domain.Entities.SessionReminderPolicy ToDomain(global::Vitarish.Appointments.Domain.Dto.SessionReminderPolicyWriteDto src);

    public partial global::Vitarish.Appointments.Domain.Dto.SessionReminderPolicyWriteDto ToWrite(global::Vitarish.Appointments.Domain.Dto.SessionReminderPolicyReadDto src);
}
