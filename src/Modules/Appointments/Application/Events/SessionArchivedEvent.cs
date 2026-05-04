using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record SessionArchivedEvent(Guid SessionId) : INotification;
