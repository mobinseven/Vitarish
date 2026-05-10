using MediatR;

namespace Vitarish.Clinical.Application.Events;

public record PatientRegisteredEvent(string PatientFhirId, string ActorId, string TenantId) : INotification;
