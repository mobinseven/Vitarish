using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record SessionOpenedEvent(Guid SessionId) : INotification;
