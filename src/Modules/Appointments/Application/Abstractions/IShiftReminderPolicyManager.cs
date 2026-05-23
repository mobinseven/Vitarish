namespace Vitarish.Appointments.Application.Abstractions;

public interface IShiftReminderPolicyManager
    : IEntityService<ShiftReminderPolicyReadDto, ShiftReminderPolicyWriteDto, ShiftReminderPolicyDescriptorDto>
{
}
