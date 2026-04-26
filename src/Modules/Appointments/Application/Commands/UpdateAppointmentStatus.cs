namespace Vitarish.Appointments.Application.Commands;

public record UpdateAppointmentStatusCommand(Guid AppointmentId, AppointmentStatus Status) : IRequest;
