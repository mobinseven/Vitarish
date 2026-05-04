using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record CapacityChangedEvent(Guid SessionId, int OldTotalSeats, int NewTotalSeats) : INotification;
