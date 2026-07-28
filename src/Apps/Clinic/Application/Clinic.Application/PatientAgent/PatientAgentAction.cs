namespace Vitarish.Clinic.Application.PatientAgent;

public enum PatientAgentActionStatus
{
    Proposed,
    Completed
}

public sealed record PatientAgentAction(
    string Id,
    string Capability,
    string ActionType,
    PatientAgentActionStatus Status,
    string ArgumentsJson,
    string Summary);
