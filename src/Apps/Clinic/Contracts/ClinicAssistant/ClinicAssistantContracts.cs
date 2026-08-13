using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vitarish.Clinic.Contracts.ClinicAssistant;

public sealed record RunClinicAssistantRequest(string Message, string? ConversationId = null);

public sealed record RunClinicAssistantResponse(
    string AgentRunId,
    string ConversationId,
    string? PreviousResponseId,
    IReadOnlyList<ClinicAssistantMessageResponse> Messages,
    IReadOnlyList<ClinicAssistantActionResponse> Actions,
    IReadOnlyList<ClinicAssistantCandidateResponse>? Candidates = null,
    bool HasMoreCandidates = false);

public sealed record ClinicAssistantCandidateResponse(
    string CandidateToken,
    string DisplayName,
    DateOnly? DateOfBirth,
    string MaskedIdentification,
    string MaskedPhone);

public sealed record ResolveClinicAssistantCandidateRequest(string AgentRunId, string CandidateToken);

public sealed record ResolveClinicAssistantCandidateResponse(Guid PatientId);

public sealed record ClinicAssistantMessageResponse(ClinicAssistantMessageResponseKind Kind, string Content);

public sealed record ClinicAssistantActionResponse(
    string Id,
    string Capability,
    string ActionType,
    ClinicAssistantActionStatus Status,
    JsonElement Arguments,
    string Summary);

public sealed record ConfirmClinicAssistantActionRequest(string ActionId);

public sealed record RejectClinicAssistantActionRequest(string ActionId, string? Reason = null);

public sealed record ClinicAssistantActionDetailsResponse(
    Guid ActionId,
    string AgentRunId,
    string ConversationId,
    string Capability,
    string ActionType,
    ClinicAssistantActionStatus Status,
    JsonElement ReviewPayload,
    DateTimeOffset ExpiresAt,
    JsonElement? CompletionPayload,
    ClinicAssistantActionFailureResponse? Failure);

public sealed record ConfirmClinicAssistantActionResponse(
    Guid ActionId,
    ClinicAssistantActionStatus Status,
    JsonElement? CompletionPayload,
    ClinicAssistantActionFailureResponse? Failure);

public sealed record RejectClinicAssistantActionResponse(
    Guid ActionId,
    ClinicAssistantActionStatus Status);

public sealed record ClinicAssistantActionFailureResponse(string Code, string Message, bool Retryable);

[JsonConverter(typeof(JsonStringEnumConverter<ClinicAssistantActionStatus>))]
public enum ClinicAssistantActionStatus
{
    Proposed,
    Confirming,
    Completed,
    Rejected,
    Expired,
    Failed
}

[JsonConverter(typeof(JsonStringEnumConverter<ClinicAssistantMessageResponseKind>))]
public enum ClinicAssistantMessageResponseKind
{
    AssistantMessage,
    InformationalToolResult,
    MissingInformationRequest,
    Failure
}
