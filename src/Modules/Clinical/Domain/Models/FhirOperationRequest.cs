namespace Vitarish.Clinical.Domain.Models;

public sealed record FhirOperationRequest
{
    public required string ResourceType { get; init; }
    public string? ResourceId { get; init; }
    public FhirInteractionType Interaction { get; init; }
    public string? PayloadJson { get; init; }
    public string? Query { get; init; }
    public string? IdempotencyKey { get; init; }
    public string? CorrelationId { get; init; }
}
