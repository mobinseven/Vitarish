namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class AppointmentsService(IMediator mediator)
    : EntityService<Appointment, AppointmentDescriptor, AppointmentReadDto, AppointmentWriteDto, AppointmentDescriptorDto>(
        mediator,
        appointment => appointment.ToDto(),
        appointment => appointment.ToDomain(),
        appointment => appointment.ToDomain()),
      IAppointmentsService
{
}
