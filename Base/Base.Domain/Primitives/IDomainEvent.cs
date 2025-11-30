namespace Vitarish.Base.Domain.Primitives;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something significant that happened in the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of this event occurrence.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the timestamp when this event occurred.
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}
