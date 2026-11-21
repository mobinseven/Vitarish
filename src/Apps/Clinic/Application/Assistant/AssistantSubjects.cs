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
}
