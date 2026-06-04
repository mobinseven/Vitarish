namespace Vitarish.Sea.Domain.Models;

public sealed class EventStats
{
    public Guid EventId { get; init; }
    public int EventItems { get; init; }
    public int NewItems { get; init; }
    public bool Unread { get; init; }
    public DateTime? LastUpdate { get; init; }
}
