using System.Text.Json;

namespace Vitarish.Intelligence.Application.Agents;

/// <summary>Versioned, provider-neutral identity and scope for one Clinic/agent-runtime execution.</summary>
public sealed record AgentRuntimeExecutionEnvelope(
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
    string? CallbackSignature = null)
{
    public const int CurrentVersion = 1;
    public const int CurrentCredentialVersion = 1;

    public AgentRuntimeExecutionEnvelope WithoutCallbackSignature() => this with { CallbackSignature = null };

    public AgentRuntimeExecutionEnvelope ForTool(string toolCallId, string toolName, string callbackSignature) =>
        this with { ToolCallId = toolCallId, ToolName = toolName, CallbackSignature = callbackSignature };

    public AgentExecutionContext ToExecutionContext()
    {
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            ["tenantId"] = TenantId,
            ["practiceId"] = PracticeId,
            ["patientId"] = SubjectKind == "patient" ? SubjectId : Guid.Empty.ToString("D"),
            ["authenticatedUserId"] = AuthenticatedUserId,
            ["permissions"] = JsonSerializer.Serialize(Permissions),
            ["role"] = Role,
            ["locale"] = Locale,
            ["timeZone"] = TimeZone,
            ["calendar"] = Calendar,
            ["currentUtc"] = CurrentUtc,
            ["currentLocal"] = CurrentLocal,
            ["conversationId"] = ConversationId,
            ["subjectKind"] = SubjectKind,
            ["subjectId"] = SubjectId,
            ["agentKey"] = AgentKey
        };
        if (PatientNationalId is not null) metadata["patientNationalId"] = PatientNationalId;
        return new AgentExecutionContext(ExecutionId, metadata) { Envelope = this };
    }

    public static AgentRuntimeExecutionEnvelope FromContext(
        AgentExecutionContext context,
        DateTimeOffset expiresAt,
        string toolCallId = "",
        string toolName = "")
    {
        IReadOnlyDictionary<string, string> metadata = context.Metadata
            ?? throw new InvalidOperationException("Agent execution metadata is required.");
        string Required(string key) => metadata.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Agent execution metadata is missing {key}.");
        string[] permissions = JsonSerializer.Deserialize<string[]>(Required("permissions"))
            ?? throw new InvalidOperationException("Agent execution permissions are invalid.");
        return new(
            CurrentVersion,
            RequiredExecutionId(context),
            Required("agentKey"),
            Required("tenantId"),
            Required("practiceId"),
            Required("subjectKind"),
            Required("subjectId"),
            Required("authenticatedUserId"),
            Required("conversationId"),
            toolCallId,
            toolName,
            CurrentCredentialVersion,
            expiresAt,
            Required("correlationId"),
            Required("role"),
            Required("locale"),
            Required("timeZone"),
            Required("calendar"),
            Required("currentUtc"),
            Required("currentLocal"),
            permissions,
            metadata.TryGetValue("patientNationalId", out string? nationalId) ? nationalId : null);

        static string RequiredExecutionId(AgentExecutionContext context) =>
            !string.IsNullOrWhiteSpace(context.ExecutionId)
                ? context.ExecutionId
                : throw new InvalidOperationException("Agent execution id is required.");
    }
}

public interface IAgentRuntimeReplayGuard
{
    Task<bool> TryClaimAsync(
        AgentRuntimeExecutionEnvelope envelope,
        CancellationToken cancellationToken = default);
}
