namespace Vitarish.Clinical.Domain.Models;

public sealed record FhirOperationResponse
{
    public Guid OperationId { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public FhirOperationStatus Status { get; init; }
    public FhirFailureKind FailureKind { get; init; }
    public string? FailureMessage { get; init; }
    public int? ResponseStatusCode { get; init; }
    public int? ResultCount { get; init; }
    public string? ResponseJson { get; init; }
    public string? OutcomeJson { get; init; }
    public string? RemoteVersionId { get; init; }
    public string? ETag { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
}
