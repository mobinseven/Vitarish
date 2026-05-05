using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Entities;

public class ConnectorHealthSnapshot : Entity
{
    public string Endpoint { get; set; } = string.Empty;
    public ConnectorHealthState State { get; set; } = ConnectorHealthState.Unknown;
    public bool IsReachable { get; set; }
    public string? FhirVersion { get; set; }
    public string? SoftwareName { get; set; }
    public string? SoftwareVersion { get; set; }
    public string? FailureMessage { get; set; }
    public string? SupportedResourceTypes { get; set; }
    public DateTimeOffset CheckedAt { get; set; }
}
