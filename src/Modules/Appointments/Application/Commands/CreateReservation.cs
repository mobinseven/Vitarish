namespace Vitarish.Appointments.Application.Commands;

public record CreateReservationCommand(
    Guid ShiftId,
    DateOnly Date,
    string? ReservingPartyId,
    bool CapacityOverride = false,
    TimeSpan? EstimatedSessionLength = null,
    bool IsHold = false,
    TimeSpan? HoldDuration = null) : IRequest<EntityResult<Guid>>;
