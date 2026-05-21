using Riok.Mapperly.Abstractions;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Entities;

namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for ShiftBookingPolicy owned entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Appointments.Domain.Dto.ShiftBookingPolicyReadDto ToDto(global::Vitarish.Appointments.Domain.Entities.ShiftBookingPolicy src);

    public partial global::Vitarish.Appointments.Domain.Entities.ShiftBookingPolicy ToDomain(global::Vitarish.Appointments.Domain.Dto.ShiftBookingPolicyWriteDto src);

    public partial global::Vitarish.Appointments.Domain.Dto.ShiftBookingPolicyWriteDto ToWrite(global::Vitarish.Appointments.Domain.Dto.ShiftBookingPolicyReadDto src);
}
