namespace Vitarish.Social.Contracts;
public enum SocialConnectionStatus { Active, TokenExpired, ReconnectRequired, Disabled }
public enum WebhookProcessingStatus { Pending, Processed, Failed, DeadLetter }
public record SocialConnection(Guid Id, string Provider, string ExternalAccountId, SocialConnectionStatus Status);
public record RawWebhookEvent(Guid Id, Guid SocialConnectionId, DateTimeOffset ReceivedAt, WebhookProcessingStatus ProcessingStatus, string? ProcessingError);
public interface ISocialClient : ISocialConnectionsService, ISocialWebhookGatewayService;
public interface ISocialConnectionsService { Task<IReadOnlyList<SocialConnection>> GetConnectionsAsync(CancellationToken cancellationToken); Task<RawWebhookEvent?> GetLastWebhookAsync(Guid connectionId, CancellationToken cancellationToken); }
public interface ISocialWebhookGatewayService { Task ReplayAsync(Guid rawWebhookEventId, CancellationToken cancellationToken); }
public static class SocialApiRoutes { public const string Base = "/api/modules/social"; public const string Connections = "/connections"; public const string ReplayWebhook = "/webhooks/{id:guid}/replay"; }
