namespace Vitarish.Entities.Server.Models;

public sealed record EntityNotificationArgs(
    string EntityName,
    string Id,
    EntityOperation Operation,
    DateTimeOffset OccurredAt,
    string? CorrelationId = null,
    IReadOnlyDictionary<string, object?>? Snapshot = null,
    IReadOnlyDictionary<string, (object? Old, object? New)>? Changes = null);
