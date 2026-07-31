namespace Vitarish.Clinic.Application.PatientAgent;

public sealed record PatientAgentContext(
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
    string SubjectKind = "patient");
