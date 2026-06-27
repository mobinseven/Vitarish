using Vitarish.Social.Contracts.Enums;

namespace Vitarish.Social.Contracts.Responses;

public sealed record SocialConnectionResponse(
    Guid Id,
    SocialProviderResponse Provider,
    string ExternalAccountId,
    SocialConnectionStatusResponse Status);

public sealed record SocialWebhookEventResponse(
    Guid Id,
    Guid? SocialConnectionId,
    SocialProviderResponse Provider,
    DateTimeOffset ReceivedAt,
    SocialWebhookVerificationStatusResponse VerificationStatus,
    SocialWebhookProcessingStatusResponse ProcessingStatus,
    string? ProcessingError);

