namespace Vitarish.Clinical.Domain.Models;

public sealed record AuditLogReadDto
{
    public Guid Id { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string? ActorId { get; init; }
    public string? ResourceType { get; init; }
    public string? ResourceId { get; init; }
    public string? CorrelationId { get; init; }
    public string? Reason { get; init; }
    public string? Outcome { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
