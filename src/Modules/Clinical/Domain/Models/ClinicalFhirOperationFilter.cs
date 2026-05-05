namespace Vitarish.Clinical.Domain.Models;

public sealed record ClinicalFhirOperationFilter
{
    public string? ResourceType { get; init; }
    public FhirInteractionType? Interaction { get; init; }
    public FhirOperationStatus? Status { get; init; }
    public string? SearchText { get; init; }
    public int Take { get; init; } = 50;
}
