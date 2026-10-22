using System.Text.Json.Serialization;
using Vitarish.Intelligence.Contracts;

namespace Vitarish.Landing.Contracts;

public static class LandingApiRoutes
{
    public const string Base = "/api/apps/landing";
    public const string Conversations = "/conversations";
    public const string ConversationById = "/conversations/{conversationId:guid}";
    public const string Administration = "/administration";
    public const string Features = "/features";
}

public sealed record SubmitLandingMessageRequest(string OwnerKey, string Message, string? ConversationId = null);
/// <summary><paramref name="Message"/> repeats the reply text for clients that do not read <paramref name="Reply"/>.</summary>
public sealed record LandingConversationResponse(string ConversationId, string Message, AgentInteractiveReplyResponse? Reply = null);

[JsonConverter(typeof(JsonStringEnumConverter<LandingFeatureAvailability>))]
public enum LandingFeatureAvailability { Live, InProgress, NotYet }

public sealed record LandingFeatureResponse(string Key, LandingFeatureAvailability Availability);
public sealed record LandingFeaturesResponse(IReadOnlyList<LandingFeatureResponse> Features);
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
public sealed record LandingHostBindingResponse(string Hostname, Guid? TenantId);
public sealed record SetLandingHostBindingRequest(Guid TenantId);
public sealed record SetLandingHostBindingResponse(string Hostname, Guid? PreviousTenantId, Guid TenantId, bool Changed);
