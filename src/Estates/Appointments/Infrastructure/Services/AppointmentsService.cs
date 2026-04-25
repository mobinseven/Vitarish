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
    public async Task<EntityResult> UpdateStatusAsync(Guid id, AppointmentStatus status, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateAppointmentStatusCommand(id, status), cancellationToken);
        return EntityResult.Success();
    }
}
