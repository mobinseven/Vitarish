namespace Vitarish.Landing.Contracts;

public static class LandingApiRoutes
{
    public const string Base = "/api/apps/landing";
    public const string Conversations = "/conversations";
    public const string ConversationById = "/conversations/{conversationId:guid}";
}

public sealed record SubmitLandingMessageRequest(string OwnerKey, string Message, string? ConversationId = null);
public sealed record LandingConversationResponse(string ConversationId, string Message);
public sealed record LandingError(string Code, string Message);
