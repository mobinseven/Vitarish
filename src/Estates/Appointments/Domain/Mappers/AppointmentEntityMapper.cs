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

    public partial global::Vitarish.Appointments.Domain.Dto.AppointmentWriteDto ToWrite(global::Vitarish.Appointments.Domain.Dto.AppointmentReadDto src);

    public partial global::Vitarish.Appointments.Domain.Models.AppointmentDescriptor ToDomain(global::Vitarish.Appointments.Domain.Dto.AppointmentDescriptorDto src);
}
