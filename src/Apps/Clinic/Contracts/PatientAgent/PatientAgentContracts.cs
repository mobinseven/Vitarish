using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vitarish.Clinic.Contracts.PatientAgent;

public sealed record RunPatientAgentRequest(string Message, string? ConversationId = null);

public sealed record RunPatientAgentResponse(
    string AgentRunId,
    string ConversationId,
    string? PreviousResponseId,
    IReadOnlyList<PatientAgentMessageResponse> Messages,
    IReadOnlyList<PatientAgentActionResponse> Actions,
    IReadOnlyList<PatientAgentCandidateResponse>? Candidates = null,
    bool HasMoreCandidates = false);

public sealed record PatientAgentCandidateResponse(
    string CandidateToken,
    string DisplayName,
    DateOnly? DateOfBirth,
    string MaskedIdentification,
    string MaskedPhone);

public sealed record ResolvePatientAgentCandidateRequest(string AgentRunId, string CandidateToken);

public sealed record ResolvePatientAgentCandidateResponse(Guid PatientId);

public sealed record PatientAgentMessageResponse(PatientAgentMessageResponseKind Kind, string Content);

public sealed record PatientAgentActionResponse(
    string Id,
    string Capability,
    string ActionType,
    PatientAgentActionStatus Status,
    JsonElement Arguments,
    string Summary);

public sealed record ConfirmPatientAgentActionRequest(string ActionId);

public sealed record RejectPatientAgentActionRequest(string ActionId, string? Reason = null);

public sealed record PatientAgentActionDetailsResponse(
    Guid ActionId,
    string AgentRunId,
    string ConversationId,
    string Capability,
    string ActionType,
    PatientAgentActionStatus Status,
    JsonElement ReviewPayload,
    DateTimeOffset ExpiresAt,
    JsonElement? CompletionPayload,
    PatientAgentActionFailureResponse? Failure);

public sealed record ConfirmPatientAgentActionResponse(
    Guid ActionId,
    PatientAgentActionStatus Status,
    JsonElement? CompletionPayload,
    PatientAgentActionFailureResponse? Failure);

public sealed record RejectPatientAgentActionResponse(
    Guid ActionId,
    PatientAgentActionStatus Status);

public sealed record PatientAgentActionFailureResponse(string Code, string Message, bool Retryable);

[JsonConverter(typeof(JsonStringEnumConverter<PatientAgentActionStatus>))]
public enum PatientAgentActionStatus
{
    Proposed,
    Confirming,
    Completed,
    Rejected,
    Expired,
    Failed
}

[JsonConverter(typeof(JsonStringEnumConverter<PatientAgentMessageResponseKind>))]
public enum PatientAgentMessageResponseKind
{
    AssistantMessage,
    InformationalToolResult,
    MissingInformationRequest,
    Failure
}
