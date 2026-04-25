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

        switch (request.Status)
        {
            case AppointmentStatus.Confirmed:
                entity.Confirm();
                break;
            case AppointmentStatus.Cancelled:
                entity.Cancel();
                break;
            case AppointmentStatus.Completed:
                entity.Complete();
                break;
        }

        _ = await repository.UpdateAsync(entity, cancellationToken);
    }
}
