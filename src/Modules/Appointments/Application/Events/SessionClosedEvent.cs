using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record SessionClosedEvent(Guid SessionId) : INotification;
