namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class CancelSessionReservationHandler(IEntityRepository<Appointment, AppointmentDescriptor> appointmentRepository)
    : IRequestHandler<CancelSessionReservationCommand>
{
    public async Task Handle(CancelSessionReservationCommand request, CancellationToken cancellationToken)
    {
        AppointmentDescriptor descriptor = new()
        {
            SessionId = request.SessionId,
            CreatedBy = request.UserId
        };

        IReadOnlyList<Appointment> appointments = await appointmentRepository.GetAsync(descriptor, cancellationToken);

        Appointment? active = appointments.FirstOrDefault(a => a.Status != AppointmentStatus.Cancelled);
        if (active is null)
        {
            return;
        }

        active.Status = AppointmentStatus.Cancelled;
        _ = await appointmentRepository.UpdateAsync(active, cancellationToken);
    }
}
