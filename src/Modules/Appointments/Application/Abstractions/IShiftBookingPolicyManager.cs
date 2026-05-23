namespace Vitarish.Appointments.Application.Abstractions;

public interface IShiftBookingPolicyManager
    : IEntityService<ShiftBookingPolicyReadDto, ShiftBookingPolicyWriteDto, ShiftBookingPolicyDescriptorDto>
{
}
