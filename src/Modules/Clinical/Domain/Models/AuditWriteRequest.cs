namespace Vitarish.Clinical.Domain.Models;

public sealed record AuditWriteRequest
{
    public required string Category { get; init; }
    public required string Action { get; init; }
    public string? ActorId { get; init; }
    public string? ResourceType { get; init; }
    public string? ResourceId { get; init; }
    public string? CorrelationId { get; init; }
    public string? Reason { get; init; }
    public string? Outcome { get; init; }
    public object? Details { get; init; }
}
