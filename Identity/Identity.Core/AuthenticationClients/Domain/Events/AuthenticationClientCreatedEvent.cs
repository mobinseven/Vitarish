using MediatR;

namespace Vitarish.Identity.Core.AuthenticationClients.Domain.Events;

/// <summary>
/// Raised when a new authentication client is created.
/// </summary>
public sealed record AuthenticationClientCreatedEvent(string ClientId) : INotification;
