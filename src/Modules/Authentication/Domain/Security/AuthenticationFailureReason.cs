namespace Vitarish.Authentication.Domain.Security;

public enum AuthenticationFailureReason
{
    InvalidInput,
    InvalidCredentials,
    ChallengeNotFound,
    ChallengeExpired,
    ChallengeAlreadyUsed,
    ChallengeAttemptsExhausted,
    InvalidVerificationCode,
    InvalidChallengePurpose,
    LockedOut,
    NotAllowed,
    TooManyAttempts,
    AdditionalStepRequired,
    Unauthorized,
    ProviderFailure,
    UnknownCredential,
    InvalidCredential,
    AccountDisabled
}
