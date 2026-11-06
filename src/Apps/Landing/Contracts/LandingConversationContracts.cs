using System.Text.Json.Serialization;
using Vitarish.Intelligence.Contracts;

namespace Vitarish.Landing.Contracts;

public static class LandingApiRoutes
{
    public const string Base = "/api/apps/landing";
    public const string DocumentTag = "Landing";
    public const string Conversations = "/conversations";
    public const string ConversationById = "/conversations/{conversationId:guid}";
    public const string Administration = "/administration";
    public const string Catalog = "/catalog";
    public const string OnboardingHandoff = "/onboarding-handoff";
    public const string Workspaces = "/workspaces";
    public const string WorkspaceHandoff = "/workspaces/{tenantId:guid}/handoff";
    public const string HomeConversations = "/home-conversations";
    public const string HomeConversationById = "/home-conversations/{conversationId:guid}";
}

public static class LandingApiOperations
{
    public const string ListWorkspaces = "LandingListWorkspaces";
    public const string IssueWorkspaceHandoff = "LandingIssueWorkspaceHandoff";
    public const string IssueOnboardingHandoff = "LandingIssueOnboardingHandoff";
    public const string RunHomeConversation = "LandingRunHomeConversation";
    public const string DeleteHomeConversation = "LandingDeleteHomeConversation";
}

public sealed record SubmitLandingMessageRequest(string OwnerKey, string Message, string? ConversationId = null);
/// <summary><paramref name="Message"/> repeats the reply text for clients that do not read <paramref name="Reply"/>.</summary>
public sealed record LandingConversationResponse(string ConversationId, string Message, AgentInteractiveReplyResponse? Reply = null);

[JsonConverter(typeof(JsonStringEnumConverter<LandingFeatureAvailability>))]
public enum LandingFeatureAvailability { Live, InProgress, NotYet }

public sealed record LandingFeatureResponse(string Key, LandingFeatureAvailability Availability);

[JsonConverter(typeof(JsonStringEnumConverter<LandingProofKind>))]
public enum LandingProofKind { NoShowCost, BookingPreview, PatientTimeline, Dictation, ShiftWeek, InProgress }

public sealed record LandingConsultationOptionResponse(string Key, IReadOnlyList<string> FeatureKeys, LandingProofKind? Proof);
public sealed record LandingConsultationQuestionResponse(string Key, int MaxSelections, IReadOnlyList<LandingConsultationOptionResponse> Options);
public sealed record LandingConsultationResponse(LandingConsultationQuestionResponse Kind, LandingConsultationQuestionResponse Booking, LandingConsultationQuestionResponse Pains);

/// <summary>The next-action keys the assistant may return in <see cref="AgentInteractiveReplyResponse.NextAction"/>.</summary>
public sealed record LandingNextActionsResponse(string OpenClinic, string TalkToTeam, string TryVoice);

/// <summary>Everything the page and the assistant share: features, the consultation and the next steps.</summary>
public sealed record LandingCatalogResponse(IReadOnlyList<LandingFeatureResponse> Features, LandingConsultationResponse Consultation, LandingNextActionsResponse NextActions);
public static class LandingErrorCodes
{
    public const string NotFound = "not_found";
    public const string Busy = "busy";
    public const string InvalidHistory = "invalid_history";
    public const string Conflict = "conflict";
    public const string Unavailable = "unavailable";
    public const string Validation = "validation";
}

public sealed record LandingError(string Code, string Message);

[JsonConverter(typeof(JsonStringEnumConverter<LandingAppAvailability>))]
public enum LandingAppAvailability { Live, Soon }

public sealed record LandingWorkspaceResponse(Guid TenantId, string AppId, string Name, string Hostname, bool HasLogo);
public sealed record LandingAppResponse(string AppId, string Name, LandingAppAvailability Availability, bool CanCreate);
public sealed record LandingHomeActionKeysResponse(string CreateWorkspace, string OpenWorkspace);
public sealed record LandingWorkspacesResponse(IReadOnlyList<LandingWorkspaceResponse> Workspaces, IReadOnlyList<LandingAppResponse> Apps, LandingHomeActionKeysResponse ActionKeys);

public sealed record LandingWorkspaceHandoffResponse(string Hostname, string Token);

/// <summary>Optional catalog app choice; omitted requests preserve the existing clinic setup handoff.</summary>
public sealed record LandingOnboardingHandoffRequest(string? Category, IReadOnlyList<string>? Answers, string? AppId = null);

public sealed record LandingHomeConversationRequest(string Message, string? ConversationId = null);
public sealed record LandingHomeActionTarget(string Key, string? AppId = null, Guid? TenantId = null);
/// <summary>The shared interactive reply plus a server-validated action target from the current catalog/directory.</summary>
public sealed record LandingHomeConversationResponse(string ConversationId, string Message, AgentInteractiveReplyResponse? Reply, LandingHomeActionTarget? ActionTarget);

/// <summary>A one-time ticket that signs the person in on the onboarding host, where they create their clinic.</summary>
public sealed record LandingOnboardingHandoffResponse(string Hostname, string Token);
public sealed record LandingHostBindingResponse(string Hostname, Guid? TenantId);
public sealed record SetLandingHostBindingRequest(Guid TenantId);
public sealed record SetLandingHostBindingResponse(string Hostname, Guid? PreviousTenantId, Guid TenantId, bool Changed);
