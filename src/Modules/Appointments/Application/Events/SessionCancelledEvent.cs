using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record SessionCancelledEvent(Guid SessionId) : INotification;
