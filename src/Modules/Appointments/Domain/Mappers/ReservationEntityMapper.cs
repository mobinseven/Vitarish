using Riok.Mapperly.Abstractions;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Entities;
using Vitarish.Appointments.Domain.Models;

namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for Reservation entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Appointments.Domain.Dto.ReservationReadDto ToDto(global::Vitarish.Appointments.Domain.Entities.Reservation src);

    [MapperIgnoreSource(nameof(ReservationWriteDto.SessionId))]
    [MapperIgnoreSource(nameof(ReservationWriteDto.Date))]
    public partial global::Vitarish.Appointments.Domain.Entities.Reservation ToDomain(global::Vitarish.Appointments.Domain.Dto.ReservationWriteDto src);

    public partial global::Vitarish.Appointments.Domain.Dto.ReservationWriteDto ToWrite(global::Vitarish.Appointments.Domain.Dto.ReservationReadDto src);

    public partial global::Vitarish.Appointments.Domain.Models.ReservationDescriptor ToDomain(global::Vitarish.Appointments.Domain.Dto.ReservationDescriptorDto src);
}
