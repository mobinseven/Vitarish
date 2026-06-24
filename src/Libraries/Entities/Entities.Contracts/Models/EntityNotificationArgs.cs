namespace Vitarish.Entities.Contracts;

public sealed record EntityNotificationArgs
{
    public required string EntityName { get; set; }
    public required string Id { get; set; }
    public EntityOperation Operation { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? CorrelationId { get; set; }
    public IReadOnlyDictionary<string, object?>? Snapshot { get; set; }
    public IReadOnlyDictionary<string, (object? Old, object? New)>? Changes { get; set; }
}
