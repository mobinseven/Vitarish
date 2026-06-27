namespace Vitarish.Social.Contracts.Enums;

public enum SocialProviderResponse
{
    Telegram = 1,
    Instagram = 2,
}

public enum SocialConnectionStatusResponse
{
    Active = 1,
    Disconnected = 2,
    TokenExpired = 3,
    ReconnectRequired = 4,
}

public enum SocialWebhookProcessingStatusResponse
{
    Pending = 1,
    Processing = 2,
    Processed = 3,
    Failed = 4,
    DeadLetter = 5,
}

public enum SocialWebhookVerificationStatusResponse
{
    Pending = 1,
    Verified = 2,
    Failed = 3,
}

public enum SocialErrorKind
{
    None,
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Timeout,
    Cancellation,
    Transport,
    Deserialization,
    UnexpectedFailure,
}

public enum SocialReplayWebhookStatus
{
    Queued,
    ValidationFailure,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Timeout,
    Cancellation,
    TransportFailure,
    DeserializationFailure,
    UnexpectedFailure,
}

