using Riok.Mapperly.Abstractions;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Models;

namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for ShiftReservation keyless read model.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Appointments.Domain.Dto.ShiftReservationReadDto ToDto(global::Vitarish.Appointments.Domain.Models.ShiftReservation src);
}
