using MediatR;

namespace Vitarish.Parties.Domain.Events;

/// <summary>
/// Raised after a save that added, changed, or removed tenant indicators. Lists every
/// canonical hostname whose owner, method, or status may now differ.
/// </summary>
public sealed record TenantHostnamesChangedEvent(IReadOnlyCollection<string> Hostnames) : INotification;
