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
    /// <summary>
    /// Overrides the standard add path to route through <see cref="CreateAppointmentCommand"/>,
    /// which validates the session and sets <see cref="Appointment.ScheduledAt"/> from it.
    /// </summary>
    async Task<EntityResult<AppointmentReadDto>> IEntityService<AppointmentReadDto, AppointmentWriteDto, AppointmentDescriptorDto>.AddAsync(
        AppointmentWriteDto dto, CancellationToken cancellationToken)
    {
        Appointment? entity = await mediator.Send(new CreateAppointmentCommand(dto), cancellationToken);
        return entity is null
            ? EntityResult<AppointmentReadDto>.Failure(EntityError.Unknown("Appointment was not created."))
            : EntityResult<AppointmentReadDto>.Success(entity.ToDto());
    }

    public async Task<EntityResult> UpdateStatusAsync(Guid id, AppointmentStatus status, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateAppointmentStatusCommand(id, status), cancellationToken);
        return EntityResult.Success();
    }
}
