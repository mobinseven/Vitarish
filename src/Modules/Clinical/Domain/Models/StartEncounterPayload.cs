namespace Vitarish.Clinical.Domain.Models;

public sealed record StartEncounterPayload
{
    public required string EncounterClass { get; init; }
    public string? ServiceType { get; init; }
    public string? ReasonCode { get; init; }
    public string? LocationFhirReference { get; init; }
    public string? PractitionerFhirReference { get; init; }
    public DateTimeOffset? PeriodStart { get; init; }
}
