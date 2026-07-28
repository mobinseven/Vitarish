namespace Vitarish.Clinic.Application.PatientAgent;

public enum PatientAgentMessageKind
{
    AssistantMessage,
    InformationalToolResult,
    MissingInformationRequest,
    Failure
}

public sealed record PatientAgentMessage(PatientAgentMessageKind Kind, string Content);
