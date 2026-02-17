namespace Vitarish.Entities.Server.Models;

public sealed class EntityNotificationArgs : EventArgs
{
    public string EntityName { get; set; }
    public string Id { get; set; }
    public EntityOperation Operation { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? CorrelationId { get; set; }
    public IReadOnlyDictionary<string, object?>? Snapshot { get; set; }
    public IReadOnlyDictionary<string, (object? Old, object? New)>? Changes { get; set; }
}
