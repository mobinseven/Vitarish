using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Entities;
using Vitarish.Appointments.Domain.Models;

namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for AppointmentSession entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Appointments.Domain.Dto.AppointmentSessionReadDto ToDto(global::Vitarish.Appointments.Domain.Entities.AppointmentSession src);

    public partial global::Vitarish.Appointments.Domain.Entities.AppointmentSession ToDomain(global::Vitarish.Appointments.Domain.Dto.AppointmentSessionWriteDto src);

    public partial global::Vitarish.Appointments.Domain.Dto.AppointmentSessionWriteDto ToWrite(global::Vitarish.Appointments.Domain.Dto.AppointmentSessionReadDto src);

    public partial global::Vitarish.Appointments.Domain.Models.AppointmentSessionDescriptor ToDomain(global::Vitarish.Appointments.Domain.Dto.AppointmentSessionDescriptorDto src);
}
