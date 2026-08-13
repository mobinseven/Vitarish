namespace Vitarish.Intelligence.Contracts;

/// <summary>Transport contract for the anonymous, signed agent-runtime callback.</summary>
public sealed record AgentRuntimeExecutionEnvelopeContract(
    int Version,
    string ExecutionId,
    string AgentKey,
    string TenantId,
    string PracticeId,
    string SubjectKind,
    string SubjectId,
    string AuthenticatedUserId,
    string ConversationId,
    string ToolCallId,
    string ToolName,
    int CredentialVersion,
    DateTimeOffset ExpiresAt,
    string CorrelationId,
    string Role,
    string Locale,
    string TimeZone,
    string Calendar,
    string CurrentUtc,
    string CurrentLocal,
    string[] Permissions,
    string? PatientNationalId = null,
    string? CallbackSignature = null);

public sealed record InternalAgentToolExecutionRequest(
    AgentRuntimeExecutionEnvelopeContract ExecutionEnvelope,
    string ArgumentsJson);

public sealed record InternalAgentToolExecutionResponse(string CallId, string Output, bool IsError = false);
