namespace Vitarish.Clinical.Domain.Models;

public sealed record AuditLogFilter
{
    public string? Category { get; init; }
    public string? Action { get; init; }
    public string? ActorId { get; init; }
    public string? ResourceType { get; init; }
    public string? ResourceId { get; init; }
    public DateTimeOffset? Since { get; init; }
    public int Take { get; init; } = 50;
}
