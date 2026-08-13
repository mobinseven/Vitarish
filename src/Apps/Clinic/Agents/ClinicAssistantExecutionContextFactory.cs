using System.Text.Json;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Agents;

public sealed class ClinicAssistantExecutionContextFactory(TimeProvider timeProvider)
{
    public DateTimeOffset CurrentUtc => timeProvider.GetUtcNow();

    public AgentExecutionContext Create(ClinicWorkspaceContext context)
    {
        DateTimeOffset currentUtc = timeProvider.GetUtcNow();
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(context.TimeZone);
        DateTimeOffset currentLocal = TimeZoneInfo.ConvertTime(currentUtc, timeZone);
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            ["tenantId"] = context.PracticeId,
            ["practiceId"] = context.PracticeId,
            ["patientId"] = context.PatientId.ToString("D"),
            ["authenticatedUserId"] = context.AuthenticatedUserId,
            ["permissions"] = JsonSerializer.Serialize(context.Permissions),
            ["role"] = context.Role,
            ["locale"] = context.Locale,
            ["calendar"] = context.Calendar,
            ["timeZone"] = context.TimeZone,
            ["currentUtc"] = currentUtc.ToString("O"),
            ["currentLocal"] = currentLocal.ToString("O"),
            ["conversationId"] = context.ConversationId,
            ["subjectKind"] = context.SubjectKind,
            ["subjectId"] = SubjectId(context),
            ["agentKey"] = AgentKey(context)
        };
        if (!string.IsNullOrWhiteSpace(context.PatientNationalId)) metadata["patientNationalId"] = context.PatientNationalId;
        return new(Guid.NewGuid().ToString("N"), metadata);
    }

    public ClinicWorkspaceContext ValidateAndRebuild(ClinicWorkspaceContext expected, AgentExecutionContext supplied)
    {
        if (string.IsNullOrWhiteSpace(supplied.ExecutionId) || supplied.Envelope is not { } envelope ||
            !string.Equals(envelope.ExecutionId, supplied.ExecutionId, StringComparison.Ordinal))
            throw new InvalidOperationException("A signed Clinic execution envelope is required.");
        IReadOnlyDictionary<string, string> metadata = supplied.Metadata
            ?? throw new InvalidOperationException("Agent execution metadata is required.");
        string Required(string name) => metadata.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Agent execution metadata is missing {name}.");

        string subjectKind = Required("subjectKind");
        if (!string.Equals(subjectKind, expected.SubjectKind, StringComparison.Ordinal) ||
            !string.Equals(Required("tenantId"), expected.PracticeId, StringComparison.Ordinal) ||
            !string.Equals(Required("practiceId"), expected.PracticeId, StringComparison.Ordinal) ||
            !string.Equals(Required("authenticatedUserId"), expected.AuthenticatedUserId, StringComparison.Ordinal) ||
            !string.Equals(Required("agentKey"), AgentKey(expected), StringComparison.Ordinal) ||
            !string.Equals(Required("conversationId"), expected.ConversationId, StringComparison.Ordinal) ||
            !string.Equals(Required("subjectId"), SubjectId(expected), StringComparison.Ordinal))
            throw new InvalidOperationException("Agent execution metadata does not match the trusted Clinic scope.");

        if (!string.Equals(envelope.AgentKey, AgentKey(expected), StringComparison.Ordinal) ||
            !string.Equals(envelope.SubjectKind, expected.SubjectKind, StringComparison.Ordinal) ||
            !string.Equals(envelope.SubjectId, SubjectId(expected), StringComparison.Ordinal) ||
            !string.Equals(envelope.TenantId, expected.PracticeId, StringComparison.Ordinal) ||
            !string.Equals(envelope.PracticeId, expected.PracticeId, StringComparison.Ordinal) ||
            !string.Equals(envelope.AuthenticatedUserId, expected.AuthenticatedUserId, StringComparison.Ordinal) ||
            !string.Equals(envelope.ConversationId, expected.ConversationId, StringComparison.Ordinal))
            throw new InvalidOperationException("The signed Clinic execution envelope does not match the trusted scope.");

        Guid patientId = Guid.Parse(Required("patientId"));
        if (subjectKind == "patient" && patientId != expected.PatientId ||
            subjectKind == "practice" && patientId != Guid.Empty)
            throw new InvalidOperationException("Agent execution patient scope is invalid.");

        return expected with
        {
            SubjectKind = subjectKind,
            AgentKey = Required("agentKey")
        };
    }

    public static string AgentKey(ClinicWorkspaceContext context) =>
        string.IsNullOrWhiteSpace(context.AgentKey)
            ? context.SubjectKind == "practice" ? "clinic.assistant.operations" : "clinic.assistant.patient"
            : context.AgentKey;

    public static string SubjectId(ClinicWorkspaceContext context) =>
        context.SubjectKind == "practice" ? context.PracticeId : context.PatientId.ToString("D");
}
