namespace Vitarish.Clinical.Domain.Models;

public sealed record FhirMappingDefinition
{
    public string LocalEntityType { get; init; } = string.Empty;
    public string ResourceType { get; init; } = string.Empty;
    public bool AllowRead { get; init; } = true;
    public bool AllowWrite { get; init; }
    public string? ProjectionName { get; init; }
}
