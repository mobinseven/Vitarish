using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Entities;
using Vitarish.Appointments.Domain.Models;

namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for SessionReminderPolicy entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Appointments.Domain.Dto.SessionReminderPolicyReadDto ToDto(global::Vitarish.Appointments.Domain.Entities.SessionReminderPolicy src);

    public partial global::Vitarish.Appointments.Domain.Entities.SessionReminderPolicy ToDomain(global::Vitarish.Appointments.Domain.Dto.SessionReminderPolicyWriteDto src);

    public partial global::Vitarish.Appointments.Domain.Dto.SessionReminderPolicyWriteDto ToWrite(global::Vitarish.Appointments.Domain.Dto.SessionReminderPolicyReadDto src);

    public partial global::Vitarish.Appointments.Domain.Models.SessionReminderPolicyDescriptor ToDomain(global::Vitarish.Appointments.Domain.Dto.SessionReminderPolicyDescriptorDto src);
}
