namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for ShiftBookingPolicy entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Appointments.Domain.Dto.ShiftBookingPolicyReadDto ToRead(global::Vitarish.Appointments.Domain.Entities.ShiftBookingPolicy src);

    public partial global::Vitarish.Appointments.Domain.Entities.ShiftBookingPolicy ToEntity(global::Vitarish.Appointments.Domain.Dto.ShiftBookingPolicyWriteDto src);

    public partial global::Vitarish.Appointments.Domain.Dto.ShiftBookingPolicyWriteDto ToWrite(global::Vitarish.Appointments.Domain.Dto.ShiftBookingPolicyReadDto src);

    public partial global::Vitarish.Appointments.Domain.Models.ShiftBookingPolicyDescriptor ToDescriptor(global::Vitarish.Appointments.Domain.Dto.ShiftBookingPolicyDescriptorDto src);
}
