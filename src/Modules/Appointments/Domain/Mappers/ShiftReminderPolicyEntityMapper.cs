namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for ShiftReminderPolicy entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Appointments.Domain.Dto.ShiftReminderPolicyReadDto ToRead(global::Vitarish.Appointments.Domain.Entities.ShiftReminderPolicy src);

    public partial global::Vitarish.Appointments.Domain.Entities.ShiftReminderPolicy ToEntity(global::Vitarish.Appointments.Domain.Dto.ShiftReminderPolicyWriteDto src);

    public partial global::Vitarish.Appointments.Domain.Dto.ShiftReminderPolicyWriteDto ToWrite(global::Vitarish.Appointments.Domain.Dto.ShiftReminderPolicyReadDto src);

    public partial global::Vitarish.Appointments.Domain.Models.ShiftReminderPolicyDescriptor ToDescriptor(global::Vitarish.Appointments.Domain.Dto.ShiftReminderPolicyDescriptorDto src);
}
