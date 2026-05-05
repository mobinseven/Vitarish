namespace Vitarish.Clinical.Domain.Models;

public sealed record FhirCapabilitySummary
{
    public string Endpoint { get; init; } = string.Empty;
    public string? FhirVersion { get; init; }
    public string? SoftwareName { get; init; }
    public string? SoftwareVersion { get; init; }
    public IReadOnlyList<string> SupportedResourceTypes { get; init; } = [];
}
