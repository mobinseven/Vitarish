using MediatR;

namespace Vitarish.ApiClients.Domain.Events;

/// <summary>
/// Raised when a new API client is created.
/// </summary>
public sealed record ApiClientCreatedEvent(string ClientId) : INotification;
