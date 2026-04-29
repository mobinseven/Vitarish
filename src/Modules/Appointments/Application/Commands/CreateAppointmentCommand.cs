namespace Vitarish.Appointments.Application.Commands;

/// <summary>
/// Creates an appointment after validating the target session is active and has available capacity.
/// </summary>
public record CreateAppointmentCommand(AppointmentWriteDto Dto) : IRequest<Appointment?>;
