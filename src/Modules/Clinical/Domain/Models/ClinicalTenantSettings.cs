namespace Vitarish.Clinical.Domain.Models;

public sealed record ClinicalTenantSettings
{
    public bool Enabled { get; init; }
    public string? EndpointOverride { get; init; }
    public string? Environment { get; init; }
    public string? Organization { get; init; }
    public string? Clinic { get; init; }
    public bool RequireAccessReason { get; init; } = true;
    public IReadOnlyList<string> AllowedResourceTypes { get; init; } = [];
}
