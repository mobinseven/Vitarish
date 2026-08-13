namespace Vitarish.Clinic.Application.Assistant;

public sealed record AssistantContext(
    string PracticeId,
    Guid PatientId,
    string AuthenticatedUserId,
    IReadOnlySet<string> Permissions,
    string Role,
    string Locale,
    string TimeZone,
    string ConversationId,
    string? PatientNationalId = null,
    string Calendar = "gregory",
    bool IsNewConversation = false,
    string SubjectKind = "patient",
    string? AgentKey = null);
