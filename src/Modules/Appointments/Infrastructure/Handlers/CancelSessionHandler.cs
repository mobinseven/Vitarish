namespace Vitarish.Appointments.Infrastructure.Handlers;

/// <summary>
/// Cancels a session and bulk-cancels all non-completed appointments belonging to it.
/// </summary>
internal class CancelSessionHandler(
    IEntityRepository<Session, SessionDescriptor> sessionRepository,
    IEntityRepository<Appointment, AppointmentDescriptor> appointmentRepository,
    AppointmentsDbContext dbContext)
    : IRequestHandler<CancelSessionCommand>
{
    public async Task Handle(CancelSessionCommand request, CancellationToken cancellationToken)
    {
        Session? session = await sessionRepository.GetAsync(request.SessionId, cancellationToken);
        if (session is null)
        {
            return;
        }

        session.Cancel();
        _ = await sessionRepository.UpdateAsync(session, cancellationToken);

        List<Appointment> pendingAppointments = await dbContext.Appointments
            .Where(a => a.SessionId == request.SessionId
                     && a.Status != AppointmentStatus.Cancelled
                     && a.Status != AppointmentStatus.Completed)
            .ToListAsync(cancellationToken);

        foreach (Appointment appointment in pendingAppointments)
        {
            appointment.Cancel();
        }

        if (pendingAppointments.Count > 0)
        {
            await appointmentRepository.UpdateRangeAsync(pendingAppointments, cancellationToken);
        }
    }
}
