namespace Vitarish.Clinical.Domain.Models;

public sealed record ConnectorHealthReport
{
    public bool IsConfigured { get; init; }
    public bool IsReachable { get; init; }
    public string Endpoint { get; init; } = string.Empty;
    public ConnectorHealthState State { get; init; } = ConnectorHealthState.Unknown;
    public DateTimeOffset CheckedAt { get; init; }
    public string? FhirVersion { get; init; }
    public string? SoftwareName { get; init; }
    public string? SoftwareVersion { get; init; }
    public string? FailureMessage { get; init; }
    public IReadOnlyList<string> SupportedResourceTypes { get; init; } = [];
}
