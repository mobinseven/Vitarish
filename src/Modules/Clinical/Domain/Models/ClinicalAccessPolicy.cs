namespace Vitarish.Clinical.Domain.Models;

public sealed record ClinicalAccessPolicy
{
    public bool Enabled { get; init; }
    public bool AllowReads { get; init; } = true;
    public bool AllowWrites { get; init; }
    public bool AllowPatientSelfAccess { get; init; }
    public bool RequireAccessReason { get; init; } = true;
    public IReadOnlyList<string> AllowedResourceTypes { get; init; } = [];
    public IReadOnlyList<string> AllowedRoles { get; init; } = [];
}
