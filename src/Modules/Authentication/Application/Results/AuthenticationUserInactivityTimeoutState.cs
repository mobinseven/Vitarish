namespace Vitarish.Authentication.Application.Results;

public sealed record AuthenticationUserInactivityTimeoutState(long Version, int? InactivityTimeoutMinutes);
