namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for ShiftBookingPolicy entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Appointments.Domain.Dto.ShiftBookingPolicyReadDto ToDto(global::Vitarish.Appointments.Domain.Entities.ShiftBookingPolicy src);

    public partial global::Vitarish.Appointments.Domain.Entities.ShiftBookingPolicy ToDomain(global::Vitarish.Appointments.Domain.Dto.ShiftBookingPolicyWriteDto src);

    public partial global::Vitarish.Appointments.Domain.Dto.ShiftBookingPolicyWriteDto ToWrite(global::Vitarish.Appointments.Domain.Dto.ShiftBookingPolicyReadDto src);

    public partial global::Vitarish.Appointments.Domain.Models.ShiftBookingPolicyDescriptor ToDomain(global::Vitarish.Appointments.Domain.Dto.ShiftBookingPolicyDescriptorDto src);
}
