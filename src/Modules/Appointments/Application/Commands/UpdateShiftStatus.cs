using MediatR;

namespace Vitarish.Appointments.Application.Commands;

public record UpdateShiftStatusCommand(Guid ShiftId, ShiftStatus Status) : IRequest;
