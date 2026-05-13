using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Entities;

namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for ShiftReminderPolicy owned entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Appointments.Domain.Dto.ShiftReminderPolicyReadDto ToDto(global::Vitarish.Appointments.Domain.Entities.ShiftReminderPolicy src);

    public partial global::Vitarish.Appointments.Domain.Entities.ShiftReminderPolicy ToDomain(global::Vitarish.Appointments.Domain.Dto.ShiftReminderPolicyWriteDto src);

    public partial global::Vitarish.Appointments.Domain.Dto.ShiftReminderPolicyWriteDto ToWrite(global::Vitarish.Appointments.Domain.Dto.ShiftReminderPolicyReadDto src);
}
