using MediatR;

namespace Vitarish.Appointments.Application.Commands;

public record CreateReservationCommand(
    Guid SessionId,
    DateOnly Date,
    string? ReservingPartyId,
    bool IsHold = false,
    TimeSpan? HoldDuration = null,
    Guid? SessionOccurrenceId = null) : IRequest<EntityResult<Guid>>;
