namespace Vitarish.Social.Domain.Models;

[EntityDescriptorFor(typeof(RawWebhookEvent))]
public record RawWebhookEventDescriptor : EntityDescriptor
{
    public SocialProvider? Provider { get; init; }
    public Guid? ConnectionId { get; init; }
    public string? IdempotencyKey { get; init; }
    public WebhookProcessingStatus? ProcessingStatus { get; init; }
}
