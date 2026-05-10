namespace Vitarish.Clinical.Domain.Models;

public sealed record FhirMappedResource
{
    public required string ResourceType { get; init; }
    public required string ResourceJson { get; init; }
    public string? ResourceId { get; init; }
    public bool IsTransactionEntry { get; init; }
}
