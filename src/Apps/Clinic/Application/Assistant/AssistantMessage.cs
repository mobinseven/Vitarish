namespace Vitarish.Clinic.Application.Assistant;

public enum AssistantMessageKind
{
    AssistantMessage,
    InformationalToolResult,
    MissingInformationRequest,
    Failure
}

public sealed record AssistantMessage(AssistantMessageKind Kind, string Content);
