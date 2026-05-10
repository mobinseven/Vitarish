namespace Vitarish.Audit.Application;

public sealed record AuditEntryDescriptor
{
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public string? ActorId { get; init; }
    public int? Skip { get; init; }
    public int? Take { get; init; }
}
