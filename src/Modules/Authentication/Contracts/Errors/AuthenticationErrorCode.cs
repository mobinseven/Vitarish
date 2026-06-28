namespace Vitarish.Authentication.Contracts.Errors;

public enum AuthenticationErrorCode { InvalidRequest, InvalidCredentials, ChallengeExpired, ChallengeAlreadyUsed, TooManyAttempts, UserLockedOut, ExternalProviderUnavailable, Unauthorized, Unknown }
