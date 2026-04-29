using Riok.Mapperly.Abstractions;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Entities;
using Vitarish.Appointments.Domain.Models;

namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for Appointment entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Appointments.Domain.Dto.AppointmentReadDto ToDto(global::Vitarish.Appointments.Domain.Entities.Appointment src);

    public partial global::Vitarish.Appointments.Domain.Entities.Appointment ToDomain(global::Vitarish.Appointments.Domain.Dto.AppointmentWriteDto src);

    /// <summary>
    /// ScheduledAt is not part of the write DTO (it comes from the session);
    /// ignore it when converting a read DTO back to a write DTO.
    /// </summary>
    [MapperIgnoreSource(nameof(AppointmentReadDto.ScheduledAt))]
    public partial global::Vitarish.Appointments.Domain.Dto.AppointmentWriteDto ToWrite(global::Vitarish.Appointments.Domain.Dto.AppointmentReadDto src);

    public partial global::Vitarish.Appointments.Domain.Models.AppointmentDescriptor ToDomain(global::Vitarish.Appointments.Domain.Dto.AppointmentDescriptorDto src);

    public partial global::Vitarish.Appointments.Domain.Dto.SessionReadDto ToDto(global::Vitarish.Appointments.Domain.Entities.Session src);

    public partial global::Vitarish.Appointments.Domain.Entities.Session ToDomain(global::Vitarish.Appointments.Domain.Dto.SessionWriteDto src);

    /// <summary>
    /// ReservationCount is computed externally; ignore it when converting a read DTO back to a write DTO.
    /// </summary>
    [MapperIgnoreSource(nameof(SessionReadDto.ReservationCount))]
    public partial global::Vitarish.Appointments.Domain.Dto.SessionWriteDto ToWrite(global::Vitarish.Appointments.Domain.Dto.SessionReadDto src);

    public partial global::Vitarish.Appointments.Domain.Models.SessionDescriptor ToDomain(global::Vitarish.Appointments.Domain.Dto.SessionDescriptorDto src);
}
