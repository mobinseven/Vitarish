using MediatR;

namespace Vitarish.Appointments.Application.Commands;

public record CreateReservationCommand(
    Guid ShiftId,
    DateOnly Date,
    string? ReservingPartyId,
    bool IsHold = false,
    TimeSpan? HoldDuration = null) : IRequest<EntityResult<Guid>>;
