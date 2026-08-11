namespace Vitarish.ProjectManagement.Contracts;

using System.Text.Json;
using System.Text.Json.Serialization;

public static class ProjectManagementApiRoutes
{
    public const string Base = "/api/apps/project-management";
    public const string Events = "/events/{eventId:guid}";
    public const string Actions = Events + "/actions/{actionId:guid}";
    public const string ConfirmAction = Actions + "/confirm";
    public const string RejectAction = Actions + "/reject";
    public const string Conversation = Events + "/conversations/{conversationId:guid}";
}

public sealed record RejectProjectManagementActionRequest(string? Reason = null);
public sealed record ProjectManagementActionReview(
    string Operation,
    string EventId,
    string? AttachmentId,
    string ProposedValuesJson,
    IReadOnlyList<ProjectManagementActionReviewChange>? Changes = null,
    bool IsDestructive = false,
    string? Warning = null);
public sealed record ProjectManagementActionReviewChange(string Field, string? ProposedValue);
public sealed record ProjectManagementActionResponse(string ActionId, string Status, ProjectManagementActionReview Review, string? CompletionPayload = null, string? FailureCode = null, string? RejectionReason = null);
public sealed record ProjectManagementThreadDisplay(string Id, string Title, string? LatestTextPreview, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string Transcript, IReadOnlyList<ProjectManagementThreadTurn>? Turns = null);
public sealed record ProjectManagementThreadTurn(string User, string Assistant, IReadOnlyCollection<Guid> PreparedActionIds, IReadOnlyList<ProjectManagementPresentationPart> Parts);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ProjectManagementTextPresentationPart), "text")]
[JsonDerivedType(typeof(ProjectManagementResourceCardPresentationPart), "resource-card")]
public abstract record ProjectManagementPresentationPart;
public sealed record ProjectManagementTextPresentationPart(string Text) : ProjectManagementPresentationPart;
public sealed record ProjectManagementResourceCardPresentationPart(string Family, string Operation, string? Id, string? ParentId, string RouteKind, IReadOnlyDictionary<string, string> RouteCoordinates, DateTimeOffset CapturedAt, IReadOnlyDictionary<string, JsonElement> Fields, IReadOnlyList<IReadOnlyDictionary<string, JsonElement>> Items) : ProjectManagementPresentationPart;
