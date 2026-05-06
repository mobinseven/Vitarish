namespace Vitarish.Clinical.Domain.Models;

public sealed record ClinicalActionRequest
{
    public required string ActorId { get; init; }
    public required string TenantId { get; init; }
    public required string PatientFhirReference { get; init; }
    public string? EncounterFhirReference { get; init; }
    public required ClinicalActionType ActionType { get; init; }
    public object? Payload { get; init; }
    public string? CorrelationId { get; init; }
    public string? AccessReason { get; init; }
}
