using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record ShiftArchivedEvent(Guid ShiftId) : INotification;
