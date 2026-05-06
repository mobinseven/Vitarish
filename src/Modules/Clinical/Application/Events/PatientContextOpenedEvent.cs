using MediatR;

namespace Vitarish.Clinical.Application.Events;

public record PatientContextOpenedEvent(string PatientRef, string ActorId, string TenantId) : INotification;
