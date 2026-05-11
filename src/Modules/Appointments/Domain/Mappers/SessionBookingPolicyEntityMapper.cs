using Riok.Mapperly.Abstractions;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Entities;

namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for SessionBookingPolicy owned entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Appointments.Domain.Dto.SessionBookingPolicyReadDto ToDto(global::Vitarish.Appointments.Domain.Entities.SessionBookingPolicy src);

    public partial global::Vitarish.Appointments.Domain.Entities.SessionBookingPolicy ToDomain(global::Vitarish.Appointments.Domain.Dto.SessionBookingPolicyWriteDto src);
}
