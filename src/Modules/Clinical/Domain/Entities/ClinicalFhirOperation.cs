using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Entities;

public sealed class ClinicalFhirOperation : Entity
{
    public string ResourceType { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public FhirInteractionType Interaction { get; set; }
    public FhirOperationStatus Status { get; set; } = FhirOperationStatus.Pending;
    public FhirFailureKind FailureKind { get; set; } = FhirFailureKind.None;
    public string CorrelationId { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public int RetryCount { get; set; }
    public string? RequestJson { get; set; }
    public string? ResponseJson { get; set; }
    public string? OutcomeJson { get; set; }
    public string? FailureMessage { get; set; }
    public int? ResponseStatusCode { get; set; }
    public int? ResultCount { get; set; }
    public string? RemoteVersionId { get; set; }
    public string? ETag { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
