namespace Vitarish.Authentication.Domain.Security;

public enum AuthenticationFailureReason { InvalidInput, InvalidCredentials, LockedOut, ChallengeExpired, ChallengeAlreadyUsed, TooManyAttempts, ProviderUnavailable, Unauthorized }
