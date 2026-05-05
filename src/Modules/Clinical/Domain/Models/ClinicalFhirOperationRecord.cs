namespace Vitarish.Clinical.Domain.Models;

public sealed record ClinicalFhirOperationRecord
{
    public Guid Id { get; init; }
    public string ResourceType { get; init; } = string.Empty;
    public string? ResourceId { get; init; }
    public FhirInteractionType Interaction { get; init; }
    public FhirOperationStatus Status { get; init; }
    public FhirFailureKind FailureKind { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public string? IdempotencyKey { get; init; }
    public int RetryCount { get; init; }
    public string? RequestJson { get; init; }
    public string? ResponseJson { get; init; }
    public string? OutcomeJson { get; init; }
    public string? FailureMessage { get; init; }
    public int? ResponseStatusCode { get; init; }
    public int? ResultCount { get; init; }
    public string? RemoteVersionId { get; init; }
    public string? ETag { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
}
