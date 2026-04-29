namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class UpdateAppointmentStatusHandler(IEntityRepository<Appointment, AppointmentDescriptor> repository)
    : IRequestHandler<UpdateAppointmentStatusCommand>
{
    public async Task Handle(UpdateAppointmentStatusCommand request, CancellationToken cancellationToken)
    {
        Appointment? entity = await repository.GetAsync(request.AppointmentId, cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.Status = request.Status;

        _ = await repository.UpdateAsync(entity, cancellationToken);
    }
}
