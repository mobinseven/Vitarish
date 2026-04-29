namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class ReserveSessionHandler(IEntityRepository<Appointment, AppointmentDescriptor> appointmentRepository)
    : IRequestHandler<ReserveSessionCommand>
{
    public async Task Handle(ReserveSessionCommand request, CancellationToken cancellationToken)
    {
        Appointment appointment = new()
        {
            SessionId = request.SessionId,
            ScheduledAt = request.SessionDateTime,
            Status = AppointmentStatus.Pending
        };

        _ = await appointmentRepository.AddAsync(appointment, cancellationToken);
    }
}
