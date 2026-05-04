using MediatR;

namespace Vitarish.Appointments.Application.Commands;

public record CreateReservationCommand(Guid SessionOccurrenceId, string? ReservingPartyId, bool IsHold = false, TimeSpan? HoldDuration = null) : IRequest<Guid>;
