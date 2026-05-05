namespace Vitarish.Clinical.Domain.Models;

public sealed record ClinicalOperationsConfiguration
{
    public IReadOnlyList<string> AllowedResourceTypes { get; init; } = [];
    public IReadOnlyList<FhirInteractionType> AllowedOperations { get; init; } = [];
}
