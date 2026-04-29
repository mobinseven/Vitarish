namespace Vitarish.Appointments.Infrastructure.Handlers;

/// <summary>
/// Validates the target session (must be active and not full) then creates the appointment,
/// setting <see cref="Appointment.ScheduledAt"/> from the session's schedule.
/// </summary>
internal class CreateAppointmentHandler(
    IEntityRepository<Appointment, AppointmentDescriptor> appointmentRepository,
    IEntityRepository<Session, SessionDescriptor> sessionRepository,
    AppointmentsDbContext dbContext)
    : IRequestHandler<CreateAppointmentCommand, Appointment?>
{
    public async Task<Appointment?> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        if (request.Dto.SessionId is not { } sessionId)
        {
            throw new EntityFailureException(EntityError.Validation("A session must be selected to create an appointment."));
        }

        Session? session = await sessionRepository.GetAsync(sessionId, cancellationToken);

        if (session is null)
        {
            throw new EntityFailureException(EntityError.NotFound("The selected session was not found.", nameof(request.Dto.SessionId)));
        }

        if (session.Status is SessionStatus.Cancelled)
        {
            throw new EntityFailureException(EntityError.Validation("Cannot reserve a seat in a cancelled session."));
        }

        int reservationCount = await dbContext.Appointments
            .AsNoTracking()
            .Where(a => a.SessionId == sessionId && a.Status != AppointmentStatus.Cancelled)
            .CountAsync(cancellationToken);

        if (session.IsFull(reservationCount))
        {
            throw new EntityFailureException(EntityError.Validation("The session is fully booked."));
        }

        Appointment appointment = request.Dto.ToDomain();
        appointment.ScheduledAt = session.ScheduledAt;

        return await appointmentRepository.AddAsync(appointment, cancellationToken);
    }
}
