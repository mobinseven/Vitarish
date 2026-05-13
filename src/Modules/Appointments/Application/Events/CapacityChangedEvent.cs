using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record CapacityChangedEvent(Guid ShiftId, int OldTotalSeats, int NewTotalSeats) : INotification;
