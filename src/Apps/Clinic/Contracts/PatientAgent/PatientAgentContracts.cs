using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vitarish.Clinic.Contracts.PatientAgent;

public sealed record RunPatientAgentRequest(string Message, string? ConversationId = null);

public sealed record RunPatientAgentResponse(
    IReadOnlyList<PatientAgentMessageResponse> Messages,
    IReadOnlyList<PatientAgentActionResponse> Actions);

public sealed record PatientAgentMessageResponse(PatientAgentMessageResponseKind Kind, string Content);

public sealed record PatientAgentActionResponse(
    string Id,
    string Capability,
    string ActionType,
    PatientAgentActionResponseStatus Status,
    JsonElement Arguments,
    string Summary);

public sealed record ConfirmPatientAgentActionRequest(string ActionId);

public sealed record RejectPatientAgentActionRequest(string ActionId, string? Reason = null);

[JsonConverter(typeof(JsonStringEnumConverter<PatientAgentMessageResponseKind>))]
public enum PatientAgentMessageResponseKind
{
    AssistantMessage,
    InformationalToolResult,
    MissingInformationRequest,
    Failure
}

[JsonConverter(typeof(JsonStringEnumConverter<PatientAgentActionResponseStatus>))]
public enum PatientAgentActionResponseStatus
{
    Proposed,
    Completed
}
