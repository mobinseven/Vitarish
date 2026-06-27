namespace Vitarish.Social.Contracts.Routing;

public static class SocialApiRoutes
{
    public const string Base = "/api/modules/social";
    public const string Connections = "/connections";
    public const string LatestWebhookForConnection = "/connections/{connectionId:guid}/webhooks/latest";
    public const string ReplayWebhook = "/webhooks/{id:guid}/replay";

    public static string LatestWebhookForConnectionPath(Guid connectionId) => LatestWebhookForConnection.Replace("{connectionId:guid}", connectionId.ToString("D"));
    public static string ReplayWebhookPath(Guid id) => ReplayWebhook.Replace("{id:guid}", id.ToString("D"));
}
