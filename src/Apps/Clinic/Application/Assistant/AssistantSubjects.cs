using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.Assistant;

/// <summary>The Clinic assistant's subject kinds and the agent that serves each one.</summary>
public static class AssistantSubjects
{
    public const string PatientKind = "patient";
    public const string PracticeKind = "practice";
    public const string PatientAgentKey = "clinic.assistant.patient";
    public const string OperationsAgentKey = "clinic.assistant.operations";
    public const string LegacyPatientAgentKey = "clinic.patient";
    public const string LegacyOperationsAgentKey = "clinic.operations";

    /// <summary>
    /// The read-only, tool-disabled operations-help entry of the practice assistant. It is a route of its own, so only
    /// the route the client chose can select it; user text never does.
    /// </summary>
    public const string OperationsHelpAgentKey = "clinic.assistant.operations.help";

    /// <summary>Every key the patient-scoped agent answers to, canonical first.</summary>
    public static IReadOnlyCollection<string> PatientAgentKeys { get; } = [PatientAgentKey, LegacyPatientAgentKey];

    /// <summary>Every key the operations agent answers to, canonical first.</summary>
    public static IReadOnlyCollection<string> OperationsAgentKeys { get; } = [OperationsAgentKey, LegacyOperationsAgentKey];

    /// <summary>True when the run was entered through <see cref="OperationsHelpAgentKey"/>.</summary>
    public static bool IsOperationsHelp(AssistantContext context) =>
        context.SubjectKind == PracticeKind &&
        string.Equals(context.AgentKey, OperationsHelpAgentKey, StringComparison.Ordinal);

    /// <summary>The canonical agent for the context's subject; legacy agent-key aliases never reach stored records.</summary>
    public static string CanonicalAgentKey(AssistantContext context) =>
        context.SubjectKind == PracticeKind ? OperationsAgentKey : PatientAgentKey;

    public static string SubjectId(AssistantContext context) =>
        context.SubjectKind == PracticeKind ? context.PracticeId : context.PatientId.ToString("D");

    /// <summary>The agent the context's conversation is stored under: the entry the request used, else the canonical agent.</summary>
    public static string AgentKey(AssistantContext context) =>
        string.IsNullOrWhiteSpace(context.AgentKey) ? CanonicalAgentKey(context) : context.AgentKey;

    /// <summary>
    /// The one owner, tenant, agent and subject scope of a stored conversation. Creating, resuming and deleting a
    /// conversation all use it, so a conversation is deletable under exactly the scope it was created with.
    /// </summary>
    public static AgentConversationScope ConversationScope(AssistantContext context) =>
        new(context.PracticeId, context.AuthenticatedUserId, AgentKey(context), context.SubjectKind, SubjectId(context));

    /// <summary>
    /// The scope earlier releases stored practice conversations of the legacy operations entry under (the subject
    /// kind as its subject id). Null for every other entry. Only deletion consults it, under the same tenant, owner,
    /// agent and subject-kind checks, so historical rows stay deletable and nothing new is written in that shape.
    /// </summary>
    public static AgentConversationScope? HistoricalConversationScope(AssistantContext context) =>
        context.SubjectKind == PracticeKind && string.Equals(AgentKey(context), LegacyOperationsAgentKey, StringComparison.Ordinal)
            ? ConversationScope(context) with { SubjectId = PracticeKind }
            : null;
}
