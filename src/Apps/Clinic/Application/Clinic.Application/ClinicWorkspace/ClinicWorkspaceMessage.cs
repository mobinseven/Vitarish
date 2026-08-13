namespace Vitarish.Clinic.Application.ClinicWorkspace;

public enum ClinicWorkspaceMessageKind
{
    AssistantMessage,
    InformationalToolResult,
    MissingInformationRequest,
    Failure
}

public sealed record ClinicWorkspaceMessage(ClinicWorkspaceMessageKind Kind, string Content);
