namespace Vitarish.Clinic.Application.Assistant;

/// <summary>The Clinic assistant's subject kinds and the agent that serves each one.</summary>
public static class AssistantSubjects
{
    public const string PatientKind = "patient";
    public const string PracticeKind = "practice";
    public const string PatientAgentKey = "clinic.assistant.patient";
    public const string OperationsAgentKey = "clinic.assistant.operations";

    /// <summary>The canonical agent for the context's subject; legacy agent-key aliases never reach stored records.</summary>
    public static string CanonicalAgentKey(AssistantContext context) =>
        context.SubjectKind == PracticeKind ? OperationsAgentKey : PatientAgentKey;

    public static string SubjectId(AssistantContext context) =>
        context.SubjectKind == PracticeKind ? context.PracticeId : context.PatientId.ToString("D");
}
